using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PipeRpc.TestClient
{
    public static class Program
    {
        public static void Main(string[] args)
        {
            using (var client = new PipeRpcClient(PipeRpcHandle.FromString(args[0]), new Service()))
            {
                client.Run();
            }
        }
    }
}
