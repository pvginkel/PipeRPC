﻿using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.IO.Pipes;
using System.Linq;
using System.Reflection;
using System.Runtime.ExceptionServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace PipeRpc
{
    public class PipeRpcClient : IDisposable
    {
        private static readonly object _staticSyncRoot = new object();
        private static IOperationContext _currentOperationContext;

        public static IOperationContext CurrentOperationContext
        {
            get
            {
                lock (_staticSyncRoot)
                {
                    return _currentOperationContext;
                }
            }
        }

        private readonly ServiceDescription _serviceDescription;
        private readonly object _service;
        private JsonTextReader _reader;
        private JsonTextWriter _writer;
        private readonly JsonSerializer _serializer;
        private bool _started;
        private CancellationTokenSource _tokenSource;
        private PendingInvoke _invoke;
        private readonly object _syncRoot = new object();
        private bool _disposed;

        public PipeRpcClient(PipeRpcHandle handle, object service)
            : this(handle, service, service.GetType())
        {
        }

        public PipeRpcClient(PipeRpcHandle handle, object service, Type serviceType)
        {
            var inStream = new AnonymousPipeClientStream(PipeDirection.In, handle.OutHandle);
            _reader = new JsonTextReader(new CustomStreamReader(inStream))
            {
                SupportMultipleContent = true,
                DateParseHandling = DateParseHandling.None
            };
            var outStream = new AnonymousPipeClientStream(PipeDirection.Out, handle.InHandle);
            _writer = new JsonTextWriter(new StreamWriter(outStream));

            _serializer = JsonSerializer.CreateDefault();

            _service = service;
            _serviceDescription = ServiceDescription.FromType(serviceType);
        }

        public void Run()
        {
            if (_disposed)
                throw new ObjectDisposedException(nameof(PipeRpcServer));
            if (_started)
                throw new InvalidOperationException("RPC server has already been started");

            _started = true;

            Exception exception = null;

            using (var queue = new BlockingCollection<IEvent>())
            {
                var thread = new Thread(() => ReadProc(queue)) { IsBackground = true };
                thread.Start();

                while (!queue.IsCompleted)
                {
                    if (!queue.TryTake(out var @event, Timeout.InfiniteTimeSpan))
                        continue;

                    if (@event is ExceptionEvent exceptionEvent)
                    {
                        exception = exceptionEvent.Exception;
                    }
                    else
                    {
                        try
                        {
                            ProcessMessage((Message)@event);
                        }
                        catch (Exception ex)
                        {
                            MessageUtils.SendException(_writer, ex);
                        }
                    }
                }

                thread.Join();
            }

            if (exception != null)
                ExceptionDispatchInfo.Capture(exception).Throw();
        }

        private void ProcessMessage(Message message)
        {
            object result;

            using (message.OperationContext)
            {
                lock (_staticSyncRoot)
                {
                    _currentOperationContext = message.OperationContext;
                }

                try
                {
                    result = message.Method.Method.Invoke(_service, message.Arguments);
                }
                finally
                {
                    lock (_staticSyncRoot)
                    {
                        _currentOperationContext = null;
                    }
                }
            }

            bool haveResult = message.Method.ReturnType != typeof(void);

            MessageUtils.SendResult(_writer, result, haveResult, _serializer);
        }

        private object SendInvoke(string name, Type resultType, MethodType type, object[] args)
        {
            PendingInvoke invoke = null;

            try
            {
                if (type == MethodType.Invoke)
                {
                    invoke = new PendingInvoke(resultType, _serializer);

                    lock (_syncRoot)
                    {
                        _invoke = invoke;
                    }
                }

                _writer.WriteStartArray();
                _writer.WriteValue(type == MethodType.Invoke ? "invoke" : "post");
                _writer.WriteValue(name);
                foreach (var arg in args)
                {
                    _serializer.Serialize(_writer, arg);
                }
                _writer.WriteEndArray();
                _writer.Flush();

                return invoke?.GetResult();
            }
            finally
            {
                if (invoke != null)
                {
                    lock (_syncRoot)
                    {
                        _invoke = null;
                    }

                    invoke.Dispose();
                }
            }
        }

        private void ReadProc(BlockingCollection<IEvent> queue)
        {
            try
            {
                while (_reader.Read())
                {
                    JsonUtil.ExpectTokenType(_reader, JsonToken.StartArray);

                    string type = _reader.ReadAsString();
                    switch (type)
                    {
                        case "quit":
                            JsonUtil.ReadEndArray(_reader);
                            return;
                        case "cancel":
                            JsonUtil.ReadEndArray(_reader);
                            _tokenSource?.Cancel();
                            break;
                        case "invoke":
                            queue.Add(ParseMessage());
                            break;
                        case "result":
                        case "exception":
                            _invoke.ParseResponse(_reader, type);
                            break;
                        default:
                            throw new PipeRpcException($"Unexpected message type '{type}'");
                    }
                }
            }
            catch (Exception ex)
            {
                queue.Add(new ExceptionEvent(ex));
            }
            finally
            {
                queue.CompleteAdding();
            }
        }

        private Message ParseMessage()
        {
            string method = _reader.ReadAsString();
            bool canCancel = _reader.ReadAsInt32() != 0;

            var serviceMethod = _serviceDescription.GetMethod(method);

            int cancellationTokenIndex = -1;
            var arguments = new object[serviceMethod.ParameterTypes.Count];
            var operationContext = new OperationContext(this);

            for (int i = 0; i < serviceMethod.ParameterTypes.Count; i++)
            {
                var parameterType = serviceMethod.ParameterTypes[i];
                if (parameterType == typeof(CancellationToken))
                {
                    if (!canCancel)
                        throw new PipeRpcException("Method has a cancellation token, but invocation does not");
                    if (cancellationTokenIndex != -1)
                        throw new PipeRpcException("Method has multiple cancellation tokens");
                    cancellationTokenIndex = i;
                }
                else if (parameterType == typeof(IOperationContext))
                {
                    arguments[i] = operationContext;
                }
                else
                {
                    JsonUtil.ReadForType(_reader, parameterType);
                    arguments[i] = _serializer.Deserialize(_reader, parameterType);
                }
            }

            JsonUtil.ReadEndArray(_reader);

            if (_tokenSource != null)
            {
                _tokenSource.Dispose();
                _tokenSource = null;
            }

            if (canCancel)
            {
                if (cancellationTokenIndex == -1)
                    throw new PipeRpcException("Invocation has a cancellation token, but method does not");

                _tokenSource = new CancellationTokenSource();
                arguments[cancellationTokenIndex] = _tokenSource.Token;
            }

            return new Message(serviceMethod, arguments, operationContext);
        }

        public void Dispose()
        {
            if (!_disposed)
            {
                if (_writer != null)
                {
                    Util.DisposeSilently(_writer);
                    _writer = null;
                }
                if (_reader != null)
                {
                    Util.DisposeSilently(_reader);
                    _reader = null;
                }
                if (_tokenSource != null)
                {
                    _tokenSource.Dispose();
                    _tokenSource = null;
                }

                _disposed = true;
            }
        }

        private interface IEvent
        {
        }

        private class Message : IEvent
        {
            public ServiceMethod Method { get; }
            public object[] Arguments { get; }
            public OperationContext OperationContext { get; }

            public Message(ServiceMethod method, object[] arguments, OperationContext operationContext)
            {
                Method = method;
                Arguments = arguments;
                OperationContext = operationContext;
            }
        }

        private class ExceptionEvent : IEvent
        {
            public Exception Exception { get; }

            public ExceptionEvent(Exception exception)
            {
                Exception = exception;
            }
        }

        private class OperationContext : IOperationContext, IDisposable
        {
            private readonly PipeRpcClient _client;
            private bool _disposed;

            public OperationContext(PipeRpcClient client)
            {
                _client = client;
            }

            public void Post(string @event, params object[] args)
            {
                if (_disposed)
                    throw new ObjectDisposedException(nameof(OperationContext));

                _client.SendInvoke(@event, null, MethodType.Post, args);
            }

            public void Invoke(string @event, params object[] args)
            {
                if (_disposed)
                    throw new ObjectDisposedException(nameof(OperationContext));

                _client.SendInvoke(@event, null, MethodType.Invoke, args);
            }

            public T Invoke<T>(string @event, params object[] args)
            {
                if (_disposed)
                    throw new ObjectDisposedException(nameof(OperationContext));

                return (T)_client.SendInvoke(@event, typeof(T), MethodType.Invoke, args);
            }

            public void Dispose()
            {
                _disposed = true;
            }
        }

        private class PendingInvoke : IDisposable
        {
            private readonly Type _resultType;
            private readonly JsonSerializer _serializer;
            private ManualResetEventSlim _event = new ManualResetEventSlim();
            private object _result;
            private Exception _exception;
            private readonly object _syncRoot = new object();
            private bool _disposed;

            public PendingInvoke(Type resultType, JsonSerializer serializer)
            {
                _resultType = resultType;
                _serializer = serializer;
            }

            public void ParseResponse(JsonReader reader, string type)
            {
                try
                {
                    switch (type)
                    {
                        case "result":
                            ParseResult(reader);
                            break;
                        case "exception":
                            ParseException(reader);
                            break;
                        default:
                            throw new PipeRpcException($"Unexpected message type '{type}'");
                    }
                }
                finally
                {
                    _event.Set();
                }
            }

            private void ParseResult(JsonReader reader)
            {
                object result = null;

                if (_resultType != null)
                    result = MessageUtils.ParseResult(reader, _resultType, _serializer);

                JsonUtil.ReadEndArray(reader);

                lock (_syncRoot)
                {
                    _result = result;
                }
            }

            private void ParseException(JsonReader reader)
            {
                var exception = MessageUtils.ParseException(reader);

                JsonUtil.ReadEndArray(reader);

                lock (_syncRoot)
                {
                    _exception = exception;
                }
            }

            public object GetResult()
            {
                _event.Wait();

                lock (_syncRoot)
                {
                    if (_exception != null)
                        throw _exception;

                    return _result;
                }
            }

            public void Dispose()
            {
                if (!_disposed)
                {
                    if (_event != null)
                    {
                        _event.Dispose();
                        _event = null;
                    }

                    _disposed = true;
                }
            }
        }
    }
}
