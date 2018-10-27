using System;
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
        private ServiceDescription _serviceDescription;
        private object _service;
        private JsonTextReader _reader;
        private JsonTextWriter _writer;
        private readonly JsonSerializer _serializer;
        private bool _started;
        private readonly object _syncRoot = new object();
        private CancellationTokenSource _tokenSource;
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

            using (var queue = new BlockingCollection<Message>())
            {
                var thread = new Thread(() => ReadProc(queue)) { IsBackground = true };
                thread.Start();

                while (!queue.IsCompleted)
                {
                    if (!queue.TryTake(out var message, Timeout.InfiniteTimeSpan))
                        continue;

                    try
                    {
                        ProcessMessage(message);
                    }
                    catch (Exception ex)
                    {
                        SendException(ex);
                    }
                }

                thread.Join();
            }
        }

        private void ProcessMessage(Message message)
        {
            object result = message.Method.Method.Invoke(_service, message.Arguments);

            _writer.WriteStartArray();
            _writer.WriteValue("result");
            _serializer.Serialize(_writer, result);
            _writer.WriteEndArray();
            _writer.Flush();
        }

        private void SendException(Exception exception)
        {
            if (exception is TargetInvocationException invocationException && invocationException.InnerException != null)
                exception = invocationException.InnerException;

            _writer.WriteStartArray();
            _writer.WriteValue("exception");
            _writer.WriteValue(exception.Message);
            _writer.WriteValue(exception.GetType().FullName);
            _writer.WriteValue(exception.StackTrace);
            _writer.WriteEndArray();
            _writer.Flush();
        }

        private void ReadProc(BlockingCollection<Message> queue)
        {
            while (_reader.Read())
            {
                JsonUtil.ExpectTokenType(_reader, JsonToken.StartArray);

                string type = _reader.ReadAsString();
                switch (type)
                {
                    case "quit":
                        JsonUtil.ReadEndArray(_reader);
                        queue.CompleteAdding();
                        return;
                    case "cancel":
                        JsonUtil.ReadEndArray(_reader);
                        Cancel();
                        break;
                    case "invoke":
                        queue.Add(ParseMessage());
                        break;
                    default:
                        throw new PipeRpcException($"Unexpected message type '{type}'");
                }
            }
        }

        private void Cancel()
        {
            lock (_syncRoot)
            {
                _tokenSource?.Cancel();
            }
        }

        private Message ParseMessage()
        {
            string method = _reader.ReadAsString();
            bool canCancel = _reader.ReadAsInt32() != 0;

            var serviceMethod = _serviceDescription.GetMethod(method);

            int cancellationTokenIndex = -1;
            var arguments = new object[serviceMethod.ParameterTypes.Count];

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
                else
                {
                    JsonUtil.Read(_reader);
                    arguments[i] = _serializer.Deserialize(_reader, parameterType);
                }
            }

            JsonUtil.ReadEndArray(_reader);

            if (canCancel)
            {
                if (cancellationTokenIndex == -1)
                    throw new PipeRpcException("Invocation has a cancellation token, but method does not");

                var tokenSource = new CancellationTokenSource();

                lock (_syncRoot)
                {
                    _tokenSource = tokenSource;
                }

                arguments[cancellationTokenIndex] = tokenSource.Token;
            }

            return new Message(serviceMethod, arguments);
        }

        public void Dispose()
        {
            if (!_disposed)
            {
                if (_writer != null)
                {
                    try
                    {
                        _writer.Close();
                    }
                    catch
                    {
                        // Ignore exceptions.
                    }

                    _writer = null;
                }
                if (_reader != null)
                {
                    try
                    {
                        _reader.Close();
                    }
                    catch
                    {
                        // Ignore exceptions.
                    }

                    _reader = null;
                }

                _disposed = true;
            }
        }

        private class Message
        {
            public ServiceMethod Method { get; }
            public object[] Arguments { get; }

            public Message(ServiceMethod method, object[] arguments)
            {
                Method = method;
                Arguments = arguments;
            }
        }
    }
}
