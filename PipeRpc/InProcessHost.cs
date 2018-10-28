using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.ExceptionServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace PipeRpc
{
    internal class InProcessHost : IClientHost
    {
        private readonly ClientStartInfo _startInfo;
        private Thread _thread;
        private Exception _exception;
        private bool _disposed;

        public bool HasExited { get; private set; }

        public event EventHandler Exited;

        public InProcessHost(ClientStartInfo startInfo)
        {
            _startInfo = startInfo;

            _thread = new Thread(ThreadProc);
            _thread.Start();
        }

        private void ThreadProc()
        {
            try
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
                    appDomain.ExecuteAssembly(_startInfo.FileName, _startInfo.Arguments.ToArray());
                }
                finally
                {
                    AppDomain.Unload(appDomain);
                }
            }
            catch (Exception exception)
            {
                _exception = exception;
            }
            finally
            {
                HasExited = true;

                OnExited();
            }
        }

        protected virtual void OnExited()
        {
            Exited?.Invoke(this, EventArgs.Empty);
        }

        public void Dispose()
        {
            if (!_disposed)
            {
                if (_thread != null)
                {
                    _thread.Join();
                    _thread = null;
                }

                _disposed = true;

                if (_exception != null)
                {
                    var exception = _exception;
                    _exception = null;

                    ExceptionDispatchInfo.Capture(exception).Throw();
                }
            }
        }
    }
}
