using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PipeRpc
{
    public class PipeRpcHandle
    {
        public static PipeRpcHandle FromString(string handle)
        {
            int pos = handle.IndexOf(',');
            return new PipeRpcHandle(handle.Substring(0, pos), handle.Substring(pos + 1));
        }

        public string InHandle { get; }
        public string OutHandle { get; }

        internal PipeRpcHandle(string inHandle, string outHandle)
        {
            InHandle = inHandle;
            OutHandle = outHandle;
        }

        public override string ToString()
        {
            return InHandle + "," + OutHandle;
        }
    }
}
