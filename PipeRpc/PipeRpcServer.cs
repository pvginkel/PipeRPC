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
        private IClientHost _clientHost;

        public PipeRpcHandle Handle { get; }

        public PipeRpcServer()
            : this(PipeRpcServerMode.Remote)
        {
        }

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
            Start(null);
        }

        public void Start(ClientStartInfo startInfo)
        {
            if (_disposed)
                throw new ObjectDisposedException(nameof(PipeRpcServer));
            if (_started)
                throw new InvalidOperationException("RPC server has already been started");

            _started = true;

            if (startInfo != null)
            {
                _clientHost = _mode == PipeRpcServerMode.Remote
                    ? (IClientHost)new OutOfProcessHost(startInfo)
                    : new InProcessHost(startInfo);

                _clientHost.Exited += _clientHost_Exited;
            }

            if (_mode == PipeRpcServerMode.Remote)
            {
                _inStream.DisposeLocalCopyOfClientHandle();
                _outStream.DisposeLocalCopyOfClientHandle();
            }

            if (_clientHost?.HasExited == true)
                throw new PipeRpcException("RPC client failed to start");
        }

        private void _clientHost_Exited(object sender, EventArgs e)
        {
            Util.DisposeSilently(_inStream);
            Util.DisposeSilently(_outStream);
        }

        public void Invoke(string method, params object[] args)
        {
            Invoke(method, null, args);
        }

        public T Invoke<T>(string method, params object[] args)
        {
            return (T)Invoke(method, typeof(T), args);
        }

        private object Invoke(string method, Type returnType, params object[] args)
        {
            if (_disposed)
                throw new ObjectDisposedException(nameof(PipeRpcServer));
            if (!_started)
                throw new InvalidOperationException("RPC server has not yet been started");

            lock (_syncRoot)
            {
                SendInvoke(method, args);
            }

            return ReceiveResult(returnType);
        }

        public void On(string @event, Action action)
        {
            On(@event, new Type[0], false, args => { action(); return null; });
        }

        public void On<T>(string @event, Action<T> action)
        {
            On(@event, new[] { typeof(T) }, false, args => { action((T)args[0]); return null; });
        }

        public void On<T1, T2>(string @event, Action<T1, T2> action)
        {
            On(@event, new[] { typeof(T1), typeof(T2) }, false, args => { action((T1)args[0], (T2)args[1]); return null; });
        }

        public void On<T1, T2, T3>(string @event, Action<T1, T2, T3> action)
        {
            On(@event, new[] { typeof(T1), typeof(T2), typeof(T3) }, false, args => { action((T1)args[0], (T2)args[1], (T3)args[2]); return null; });
        }

        public void On<T1, T2, T3, T4>(string @event, Action<T1, T2, T3, T4> action)
        {
            On(@event, new[] { typeof(T1), typeof(T2), typeof(T3), typeof(T4) }, false, args => { action((T1)args[0], (T2)args[1], (T3)args[2], (T4)args[3]); return null; });
        }

        public void On<T1, T2, T3, T4, T5>(string @event, Action<T1, T2, T3, T4, T5> action)
        {
            On(@event, new[] { typeof(T1), typeof(T2), typeof(T3), typeof(T4), typeof(T5) }, false, args => { action((T1)args[0], (T2)args[1], (T3)args[2], (T4)args[3], (T5)args[4]); return null; });
        }

        public void On<T1, T2, T3, T4, T5, T6>(string @event, Action<T1, T2, T3, T4, T5, T6> action)
        {
            On(@event, new[] { typeof(T1), typeof(T2), typeof(T3), typeof(T4), typeof(T5), typeof(T6) }, false, args => { action((T1)args[0], (T2)args[1], (T3)args[2], (T4)args[3], (T5)args[4], (T6)args[5]); return null; });
        }

        public void On<TResult>(string @event, Func<TResult> action)
        {
            On(@event, new Type[0], true, args => action());
        }

        public void On<T, TResult>(string @event, Func<T, TResult> action)
        {
            On(@event, new[] { typeof(T) }, true, args => action((T)args[0]));
        }

        public void On<T1, T2, TResult>(string @event, Func<T1, T2, TResult> action)
        {
            On(@event, new[] { typeof(T1), typeof(T2) }, true, args => action((T1)args[0], (T2)args[1]));
        }

        public void On<T1, T2, T3, TResult>(string @event, Func<T1, T2, T3, TResult> action)
        {
            On(@event, new[] { typeof(T1), typeof(T2), typeof(T3) }, true, args => action((T1)args[0], (T2)args[1], (T3)args[2]));
        }

        public void On<T1, T2, T3, T4, TResult>(string @event, Func<T1, T2, T3, T4, TResult> action)
        {
            On(@event, new[] { typeof(T1), typeof(T2), typeof(T3), typeof(T4) }, true, args => action((T1)args[0], (T2)args[1], (T3)args[2], (T4)args[3]));
        }

        public void On<T1, T2, T3, T4, T5, TResult>(string @event, Func<T1, T2, T3, T4, T5, TResult> action)
        {
            On(@event, new[] { typeof(T1), typeof(T2), typeof(T3), typeof(T4), typeof(T5) }, true, args => action((T1)args[0], (T2)args[1], (T3)args[2], (T4)args[3], (T5)args[4]));
        }

        public void On<T1, T2, T3, T4, T5, T6, TResult>(string @event, Func<T1, T2, T3, T4, T5, T6, TResult> action)
        {
            On(@event, new[] { typeof(T1), typeof(T2), typeof(T3), typeof(T4), typeof(T5), typeof(T6) }, true, args => action((T1)args[0], (T2)args[1], (T3)args[2], (T4)args[3], (T5)args[4], (T6)args[5]));
        }

        private void On(string @event, Type[] types, bool hasResult, Func<object[], object> action)
        {
            _events.Add(@event, new Event(types, action, hasResult));
        }

        private void SendInvoke(string method, object[] args)
        {
            int tokenIndex = -1;

            for (int i = 0; i < args.Length; i++)
            {
                if (args[i] is CancellationToken token)
                {
                    if (tokenIndex != -1)
                        throw new PipeRpcException("Only a single cancellation token can be provided");
                    tokenIndex = i;
                    RegisterCancellation(token);
                }
            }

            _writer.WriteStartArray();

            _writer.WriteValue("invoke");
            _writer.WriteValue(method);
            _writer.WriteValue(tokenIndex != -1 ? 1 : 0);

            for (var i = 0; i < args.Length; i++)
            {
                if (i != tokenIndex)
                    _serializer.Serialize(_writer, args[i]);
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
                        ReadInvoke(false, MethodType.Post);
                        break;
                    case "invoke":
                        ReadInvoke(returnType != null, MethodType.Invoke);
                        break;
                    default:
                        throw new PipeRpcException($"Unexpected message type '{type}'");
                }
            }

            throw new PipeRpcException("Unexpected end of stream");
        }

        private void ReadInvoke(bool expectResult, MethodType type)
        {
            string name = _reader.ReadAsString();
            if (!_events.TryGetValue(name, out var @event))
                throw new PipeRpcException($"Unexpected event '{name}'");

            if (@event.HasResult != expectResult)
            {
                if (@event.HasResult)
                    throw new PipeRpcException($"Event '{name}' has a result but none was expected");
                throw new PipeRpcException($"Event '{name}' does not have a result but one was expected");
            }

            var parameterTypes = @event.ParameterTypes;
            object[] arguments = new object[parameterTypes.Length];

            for (int i = 0; i < parameterTypes.Length; i++)
            {
                var parameterType = parameterTypes[i];

                JsonUtil.ReadForType(_reader, parameterType);
                arguments[i] = _serializer.Deserialize(_reader, parameterType);
            }

            JsonUtil.ReadEndArray(_reader);

            if (type == MethodType.Invoke)
            {
                try
                {
                    object result = @event.Action(arguments);

                    MessageUtils.SendResult(_writer, result, expectResult, _serializer);
                }
                catch (Exception ex)
                {
                    MessageUtils.SendException(_writer, ex);
                }
            }
            else
            {
                @event.Action(arguments);
            }
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
                result = MessageUtils.ParseResult(_reader, returnType, _serializer);
            }

            JsonUtil.ReadEndArray(_reader);

            return result;
        }

        private PipeRpcException ReadException()
        {
            var exception = MessageUtils.ParseException(_reader);

            JsonUtil.ReadEndArray(_reader);

            throw exception;
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
                    Util.NoThrow(() =>
                    {
                        lock (_syncRoot)
                        {
                            SendCommand("quit");
                        }
                    });
                    Util.DisposeSilently(_writer);
                    _writer = null;
                }
                if (_reader != null)
                {
                    Util.DisposeSilently(_reader);
                    _reader = null;
                }
                if (_inStream != null)
                {
                    Util.NoThrow(_inStream.DisposeLocalCopyOfClientHandle);
                    Util.DisposeSilently(_inStream);
                    _inStream = null;
                }
                if (_outStream != null)
                {
                    Util.NoThrow(_outStream.DisposeLocalCopyOfClientHandle);
                    Util.DisposeSilently(_outStream);
                    _outStream = null;
                }
                if (_clientHost != null)
                {
                    _clientHost.Exited -= _clientHost_Exited;
                    _clientHost.Dispose();
                    _clientHost = null;
                }

                _disposed = true;
            }
        }

        private class Event
        {
            public Type[] ParameterTypes { get; }
            public Func<object[], object> Action { get; }
            public bool HasResult { get; }

            public Event(Type[] parameterTypes, Func<object[], object> action, bool hasResult)
            {
                ParameterTypes = parameterTypes;
                Action = action;
                HasResult = hasResult;
            }
        }
    }
}
