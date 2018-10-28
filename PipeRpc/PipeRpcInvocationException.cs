using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PipeRpc
{
    [Serializable]
    public class PipeRpcInvocationException : Exception
    {
        public string RemoteType { get; }
        public string RemoteStackTrace { get; }

        public PipeRpcInvocationException(string message, string remoteType, string remoteStackTrace)
            : base(message)
        {
            RemoteType = remoteType;
            RemoteStackTrace = remoteStackTrace;
        }
    }
}
