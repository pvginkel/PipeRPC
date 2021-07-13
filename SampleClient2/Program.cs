using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using PipeRpc;

namespace SampleClient2
{
    public static class Program
    {
        public static void Main(string[] args)
        {
            // Start the RPC client with the handle to the RPC server
            // and an instance of the service to host.

            using (var client = new PipeRpcClient(PipeRpcHandle.FromString(args[0]), new Service()))
            {
                client.Run();
            }
        }
    }
}
