using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace SampleClient2
{
    public class Service
    {
        public int SomeMethod(int value)
        {
            return value * 2;
        }

        public bool WaitForCancel(int wait, CancellationToken cancellationToken)
        {
            cancellationToken.Register(() => Console.WriteLine("Cancelled in client 2"));

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
    }
}
