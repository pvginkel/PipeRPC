using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using PipeRpc;

namespace SampleClient
{
    public class Service
    {
        public int SimpleArguments(int number, bool boolean)
        {
            Console.WriteLine($"Received {number} and {boolean}");

            return 42;
        }

        public (int A, string B) ComplexArguments(List<(int A, bool B)> values)
        {
            Console.WriteLine($"Received {String.Join(", ", values)}");

            return (42, "Hello world!");
        }

        public bool WaitForCancel(int wait, CancellationToken cancellationToken)
        {
            for (int i = 0; i < 10; i++)
            {
                if (cancellationToken.IsCancellationRequested)
                {
                    Console.WriteLine("Cancellation requested");
                    return true;
                }

                Thread.Sleep(wait / 10);
            }

            return false;
        }

        public void PostSomeProgress(int count, IOperationContext context)
        {
            for (int i = 0; i < count; i++)
            {
                context.Post("Progress", i, $"Progress: {i}");
            }
        }

        public int StartToOtherServer(int value)
        {
            return PipeRpcClient.CurrentOperationContext.Invoke<int>("InvokeOnOtherServer", value + 2);
        }

        public bool NestedWaitForCancel(int wait, CancellationToken cancellationToken)
        {
            cancellationToken.Register(() => Console.WriteLine("Cancelled in client 1"));
            return PipeRpcClient.CurrentOperationContext.Invoke<bool>("WaitForCancelOnOtherServer", wait, cancellationToken);
        }

        public void InvokeVoidInvoke(IOperationContext context)
        {
            context.Invoke("VoidInvoke");
        }
    }
}
