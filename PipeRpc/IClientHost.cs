using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PipeRpc
{
    internal interface IClientHost : IDisposable
    {
        bool HasExited { get; }

        event EventHandler Exited;
    }
}
