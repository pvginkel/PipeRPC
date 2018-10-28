using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PipeRpc
{
    internal class OutOfProcessHost : IClientHost
    {
        private Process _process;
        private bool _disposed;

        public bool HasExited => _process.HasExited;

        public event EventHandler Exited;

        public OutOfProcessHost(ClientStartInfo startInfo)
        {
            _process = Process.Start(new ProcessStartInfo
            {
                FileName = startInfo.FileName,
                Arguments = PrintArguments(startInfo.Arguments),
                UseShellExecute = false,
                WorkingDirectory = startInfo.WorkingDirectory,
                CreateNoWindow = true
            });

            _process.Exited += (s, e) => OnExited();
            _process.EnableRaisingEvents = true;
        }

        protected virtual void OnExited()
        {
            Exited?.Invoke(this, EventArgs.Empty);
        }

        private string PrintArguments(IList<string> arguments)
        {
            var sb = new StringBuilder();

            foreach (string argument in arguments)
            {
                if (sb.Length > 0)
                    sb.Append(' ');
                sb.Append('"').Append(argument.Replace("\"", "\"\"")).Append('"');
            }

            return sb.ToString();
        }

        public void Dispose()
        {
            if (!_disposed)
            {
                if (_process != null)
                {
                    _process.WaitForExit();
                    _process.Dispose();
                    _process = null;
                }

                _disposed = true;
            }
        }
    }
}
