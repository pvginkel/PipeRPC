using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Pipes;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace PipeRpc
{
    public class PipeRpcServer : IDisposable
    {
        private readonly PipeRpcServerMode _mode;
        private bool _started;
        private AnonymousPipeServerStream _inStream;
        private AnonymousPipeServerStream _outStream;
        private readonly object _syncRoot = new object();
        private long _cookie = 1;
        private JsonTextWriter _writer;
        private JsonTextReader _reader;
        private readonly JsonSerializer _serializer;
        private readonly Dictionary<string, Event> _events = new Dictionary<string, Event>();
        private bool _disposed;

        public PipeRpcHandle Handle { get; }

        public PipeRpcServer(PipeRpcServerMode mode)
        {
            _mode = mode;

            _inStream = new AnonymousPipeServerStream(PipeDirection.In, HandleInheritability.Inheritable);
            _reader = new JsonTextReader(new CustomStreamReader(_inStream))
            {
                SupportMultipleContent = true,
                DateParseHandling = DateParseHandling.None
            };
            _outStream = new AnonymousPipeServerStream(PipeDirection.Out, HandleInheritability.Inheritable);
            _writer = new JsonTextWriter(new StreamWriter(_outStream));

            _serializer = JsonSerializer.CreateDefault();

            Handle = new PipeRpcHandle(_inStream.GetClientHandleAsString(), _outStream.GetClientHandleAsString());
        }

        public void Start()
        {
            if (_disposed)
                throw new ObjectDisposedException(nameof(PipeRpcServer));
            if (_started)
                throw new InvalidOperationException("RPC server has already been started");

            _started = true;

            if (_mode == PipeRpcServerMode.Remote)
            {
                _inStream.DisposeLocalCopyOfClientHandle();
                _outStream.DisposeLocalCopyOfClientHandle();
            }
        }

        public void Invoke(string method, params object[] args)
        {
            Invoke(method, default(CancellationToken), args);
        }

        public void Invoke(string method, CancellationToken token, params object[] args)
        {
            Invoke(method, null, token, args);
        }

        public T Invoke<T>(string method, params object[] args)
        {
            return Invoke<T>(method, default(CancellationToken), args);
        }

        public T Invoke<T>(string method, CancellationToken token, params object[] args)
        {
            return (T)Invoke(method, typeof(T), token, args);
        }

        private object Invoke(string method, Type returnType, CancellationToken token, params object[] args)
        {
            if (_disposed)
                throw new ObjectDisposedException(nameof(PipeRpcServer));
            if (!_started)
                throw new InvalidOperationException("RPC server has not yet been started");

            lock (_syncRoot)
            {
                if (token.CanBeCanceled)
                    RegisterCancellation(token);

                SendInvoke(method, token, args);
            }

            return ReceiveResult(returnType);
        }

        public void On<T>(string @event, Action<T> action)
        {
            On(@event, new[] { typeof(T) }, args => action((T)args[0]));
        }

        public void On<T1, T2>(string @event, Action<T1, T2> action)
        {
            On(@event, new[] { typeof(T1), typeof(T2) }, args => action((T1)args[0], (T2)args[1]));
        }

        public void On<T1, T2, T3>(string @event, Action<T1, T2, T3> action)
        {
            On(@event, new[] { typeof(T1), typeof(T2), typeof(T3) }, args => action((T1)args[0], (T2)args[1], (T3)args[2]));
        }

        public void On<T1, T2, T3, T4>(string @event, Action<T1, T2, T3, T4> action)
        {
            On(@event, new[] { typeof(T1), typeof(T2), typeof(T3), typeof(T4) }, args => action((T1)args[0], (T2)args[1], (T3)args[2], (T4)args[3]));
        }

        public void On<T1, T2, T3, T4, T5>(string @event, Action<T1, T2, T3, T4, T5> action)
        {
            On(@event, new[] { typeof(T1), typeof(T2), typeof(T3), typeof(T4), typeof(T5) }, args => action((T1)args[0], (T2)args[1], (T3)args[2], (T4)args[3], (T5)args[4]));
        }

        public void On<T1, T2, T3, T4, T5, T6>(string @event, Action<T1, T2, T3, T4, T5, T6> action)
        {
            On(@event, new[] { typeof(T1), typeof(T2), typeof(T3), typeof(T4), typeof(T5), typeof(T6) }, args => action((T1)args[0], (T2)args[1], (T3)args[2], (T4)args[3], (T5)args[4], (T6)args[5]));
        }

        private void On(string @event, Type[] types, Action<object[]> action)
        {
            _events.Add(@event, new Event(@event, types, action));
        }

        private void SendInvoke(string method, CancellationToken token, object[] args)
        {
            _writer.WriteStartArray();

            _writer.WriteValue("invoke");
            _writer.WriteValue(method);
            _writer.WriteValue(token.CanBeCanceled ? 1 : 0);

            foreach (object arg in args)
            {
                _serializer.Serialize(_writer, arg);
            }

            _writer.WriteEndArray();
            _writer.Flush();
        }

        private object ReceiveResult(Type returnType)
        {
            while (_reader.Read())
            {
                JsonUtil.ExpectTokenType(_reader, JsonToken.StartArray);

                string type = _reader.ReadAsString();
                switch (type)
                {
                    case "result":
                        return ReadResult(returnType);
                    case "exception":
                        throw ReadException();
                    case "quit":
                        throw ReadQuit();
                    case "post":
                        ReadPost();
                        break;
                    default:
                        throw new PipeRpcException($"Unexpected message type '{type}'");
                }
            }

            throw new PipeRpcException("Unexpected end of stream");
        }

        private void ReadPost()
        {
            string name = _reader.ReadAsString();
            if (!_events.TryGetValue(name, out var @event))
                throw new PipeRpcException($"Unexpected event '{name}'");

            var parameterTypes = @event.ParameterTypes;
            object[] arguments = new object[parameterTypes.Length];

            for (int i = 0; i < parameterTypes.Length; i++)
            {
                var parameterType = parameterTypes[i];

                JsonUtil.ReadForType(_reader, parameterType);
                arguments[i] = _serializer.Deserialize(_reader, parameterType);
            }

            JsonUtil.ReadEndArray(_reader);

            @event.Action(arguments);
        }

        private PipeRpcException ReadQuit()
        {
            JsonUtil.ReadEndArray(_reader);

            throw new PipeRpcException("RPC client went away");
        }

        private object ReadResult(Type returnType)
        {
            object result = null;

            if (returnType != null)
            {
                JsonUtil.ReadForType(_reader, returnType);
                result = _serializer.Deserialize(_reader, returnType);
            }

            JsonUtil.ReadEndArray(_reader);

            return result;
        }

        private PipeRpcException ReadException()
        {
            string message = _reader.ReadAsString();
            string type = _reader.ReadAsString();
            string stackTrace = _reader.ReadAsString();

            JsonUtil.ReadEndArray(_reader);

            throw new PipeRpcInvocationException(message, type, stackTrace);
        }

        private void RegisterCancellation(CancellationToken token)
        {
            if (_disposed)
                return;

            long cookie = ++_cookie;
            token.Register(() =>
            {
                lock (_syncRoot)
                {
                    if (cookie != _cookie)
                        return;

                    SendCommand("cancel");
                }
            });
        }

        private void SendCommand(string command)
        {
            _writer.WriteStartArray();
            _writer.WriteValue(command);
            _writer.WriteEndArray();
            _writer.Flush();
        }

        public void Dispose()
        {
            if (!_disposed)
            {
                if (_writer != null)
                {
                    lock (_syncRoot)
                    {
                        SendCommand("quit");
                    }

                    _writer.Close();
                    _writer = null;
                }
                if (_reader != null)
                {
                    _reader.Close();
                    _reader = null;
                }
                if (_inStream != null)
                {
                    try
                    {
                        _inStream.DisposeLocalCopyOfClientHandle();
                        _inStream.Dispose();
                    }
                    catch
                    {
                        // Ignore exceptions.
                    }
                    _inStream = null;
                }
                if (_outStream != null)
                {
                    try
                    {
                        _outStream.DisposeLocalCopyOfClientHandle();
                        _outStream.Dispose();
                    }
                    catch
                    {
                        // Ignore exceptions.
                    }
                    _outStream = null;
                }

                _disposed = true;
            }
        }

        private class Event
        {
            public string Name { get; }
            public Type[] ParameterTypes { get; }
            public Action<object[]> Action { get; }

            public Event(string name, Type[] parameterTypes, Action<object[]> action)
            {
                Name = name;
                ParameterTypes = parameterTypes;
                Action = action;
            }
        }
    }
}
