using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PipeRpc
{
    public class PipeRpcException : Exception
    {
        public PipeRpcException()
        {
        }

        public PipeRpcException(string message)
            : base(message)
        {
        }

        public PipeRpcException(string message, Exception innerException)
            : base(message, innerException)
        {
        }
    }
}
