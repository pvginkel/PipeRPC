using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace PipeRpc
{
    [Serializable]
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

        protected PipeRpcException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }
    }
}
