using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices.ComTypes;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using PipeRpc;

namespace SampleServer
{
    public static class Program
    {
        public static void Main(string[] args)
        {
            using (var server = new PipeRpcServer(PipeRpcServerMode.Local))
            using (var server2 = new PipeRpcServer(PipeRpcServerMode.Local))
            {
                // Start the server with the location and arguments to the RPC client.

                server.Start(new ClientStartInfo(
                    typeof(SampleClient.Program).Assembly.Location,
                    null,
                    server.Handle.ToString()
                ));

                server2.Start(new ClientStartInfo(
                    typeof(SampleClient2.Program).Assembly.Location,
                    null,
                    server2.Handle.ToString()
                ));

                // Invoke a method with simple arguments.

                Console.WriteLine("Invoke a method with simple arguments");

                int result = server.Invoke<int>("SimpleArguments", 1, true);

                Console.WriteLine($"Method returned {result}");

                // Invoke a method with complex arguments.

                Console.WriteLine("Invoke a method with complex arguments");

                var result1 = server.Invoke<(int A, string B)>("ComplexArguments", new List<(int A, bool B)> { (1, true), (2, false) });

                Console.WriteLine($"Method returned {result1}");

                // Invoke a method with a cancellation token.

                Console.WriteLine("Invoking a method with a cancellation token");

                using (var cts = new CancellationTokenSource())
                {
                    cts.CancelAfter(500);

                    var result2 = server.Invoke<bool>("WaitForCancel", 1000, cts.Token);

                    Console.WriteLine($"Method returned {result2}");
                }

                // Invoking a method that posts back progress.

                server.On<int, string>("Progress", (progress, message) => Console.WriteLine($"Progress {progress}, message {message}"));

                server.Invoke("PostSomeProgress", 5);

                // Invoke a method that invokes into the other server.

                server.On<int, int>("InvokeOnOtherServer", value => server2.Invoke<int>("SomeMethod", value));

                int result3 = server.Invoke<int>("StartToOtherServer", 42);

                Console.WriteLine($"Method returned {result3}");

                // Invoke with nested cancellation token.

                server.On<int, CancellationToken, bool>("WaitForCancelOnOtherServer", (wait, cancellationToken) =>
                {
                    cancellationToken.Register(() => Console.WriteLine("Cancelled in callback"));
                    return server2.Invoke<bool>("WaitForCancel", wait, cancellationToken);
                });

                using (var cts = new CancellationTokenSource())
                {
                    cts.CancelAfter(500);

                    bool result4 = server.Invoke<bool>("NestedWaitForCancel", 1000, cts.Token);

                    Console.WriteLine($"Method returned {result4}");
                }
            }
        }
    }
}
