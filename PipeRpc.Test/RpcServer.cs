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
        private PipeRpcServer _server;
        private Thread _thread;
        private bool _disposed;

        public RpcServer()
        {
            _server = new PipeRpcServer(PipeRpcServerMode.Local);

            _thread = new Thread(() => ThreadProc(_server.Handle));
            _thread.Start();

            _server.Start();
        }

        public int ReturnInt(int value)
        {
            return _server.Invoke<int>(nameof(ReturnInt), value);
        }

        public ComplexObject ReturnComplexObject(ComplexObject value)
        {
            return _server.Invoke<ComplexObject>(nameof(ReturnComplexObject), value);
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
                if (_server != null)
                {
                    _server.Dispose();
                    _server = null;
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
