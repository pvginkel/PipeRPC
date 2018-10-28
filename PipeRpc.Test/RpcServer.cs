using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using PipeRpc.TestClient;

namespace PipeRpc.Test
{
    public class RpcServer : IDisposable
    {
        private Thread _thread;
        private bool _disposed;

        public PipeRpcServer Server { get; private set; }

        public RpcServer()
        {
            Server = new PipeRpcServer(PipeRpcServerMode.Local);

            _thread = new Thread(() => ThreadProc(Server.Handle));
            _thread.Start();

            Server.Start();
        }

        public int ReturnInt(int value)
        {
            return Server.Invoke<int>(nameof(ReturnInt), value);
        }

        public ComplexObject ReturnComplexObject(ComplexObject value)
        {
            return Server.Invoke<ComplexObject>(nameof(ReturnComplexObject), value);
        }

        public void PostBack(int value)
        {
            Server.Invoke(nameof(PostBack), value);
        }

        public bool PostWithCancellationToken(CancellationToken token)
        {
            return Server.Invoke<bool>(nameof(PostWithCancellationToken), token);
        }

        public void PostException(string message)
        {
            Server.Invoke(nameof(PostException), message);
        }

        public DateTime ReturnDateTime(DateTime dateTime)
        {
            return Server.Invoke<DateTime>(nameof(ReturnDateTime), dateTime);
        }

        public DateTimeOffset ReturnDateTimeOffset(DateTimeOffset dateTimeOffset)
        {
            return Server.Invoke<DateTimeOffset>(nameof(ReturnDateTimeOffset), dateTimeOffset);
        }

        private void ThreadProc(PipeRpcHandle handle)
        {
            var setup = new AppDomainSetup
            {
                ApplicationBase = AppDomain.CurrentDomain.BaseDirectory,
                ApplicationName = "RPC client",
                PrivateBinPath = AppDomain.CurrentDomain.SetupInformation.PrivateBinPath,
                PrivateBinPathProbe = AppDomain.CurrentDomain.SetupInformation.PrivateBinPathProbe
            };

            var appDomain = AppDomain.CreateDomain(setup.ApplicationName, AppDomain.CurrentDomain.Evidence, setup);

            try
            {
                appDomain.ExecuteAssembly(typeof(Program).Assembly.Location, new[] { handle.ToString() });
            }
            finally
            {
                AppDomain.Unload(appDomain);
            }
        }

        public void Dispose()
        {
            if (!_disposed)
            {
                if (Server != null)
                {
                    Server.Dispose();
                    Server = null;
                }
                if (_thread != null)
                {
                    _thread.Join();
                    _thread = null;
                }

                _disposed = true;
            }
        }
    }
}
