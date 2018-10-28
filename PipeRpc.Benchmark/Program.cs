using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BenchmarkDotNet.Running;

namespace PipeRpc.Benchmark
{
    class Program
    {
        public static void Main(string[] args)
        {
            //var benchmark = new Benchmark();
            //benchmark.Setup();
            //benchmark.Cancellation();
            //benchmark.Cleanup();
            //return;

            //BenchmarkRunner.Run<ComplexReturnBenchmark>();
            //return;

            var switcher = new BenchmarkSwitcher(
                typeof(Program).Assembly
                    .GetTypes()
                    .Where(p => typeof(BenchmarkBase).IsAssignableFrom(p) && !p.IsAbstract)
                    .ToArray()
            );
            switcher.RunAllJoined();
        }
    }
}
