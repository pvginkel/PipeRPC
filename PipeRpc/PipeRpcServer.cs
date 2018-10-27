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

        public T Invoke<T>(string method, params object[] args)
        {
            return Invoke<T>(method, default(CancellationToken), args);
        }

        public T Invoke<T>(string method, CancellationToken token, params object[] args)
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

            return ReceiveResult<T>();
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

        private T ReceiveResult<T>()
        {
            JsonUtil.ReadStartArray(_reader);

            string type = _reader.ReadAsString();
            switch (type)
            {
                case "result":
                    return ReadResult<T>();
                case "exception":
                    throw ReadException();
                case "quit":
                    throw ReadQuit();
                default:
                    throw new PipeRpcException($"Unexpected message type '{type}'");
            }
        }

        private PipeRpcException ReadQuit()
        {
            JsonUtil.ReadEndArray(_reader);

            throw new PipeRpcException("RPC client went away");
        }

        private T ReadResult<T>()
        {
            JsonUtil.Read(_reader);
            var result = _serializer.Deserialize<T>(_reader);
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
    }
}
