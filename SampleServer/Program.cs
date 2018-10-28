using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using PipeRpc;

namespace SampleServer
{
    public static class Program
    {
        public static void Main(string[] args)
        {
            using (var server = new PipeRpcServer())
            {
                // Start the server with the location and arguments to the RPC client.

                server.Start(new ClientStartInfo(
                    typeof(SampleClient.Program).Assembly.Location,
                    null,
                    server.Handle.ToString()
                ));

                // Invoke a method on the client service.

                int result = server.Invoke<int>("Add", 1, 2);

                // Write the results.

                Console.WriteLine($"1 + 2 = {result}");
            }
        }
    }
}
