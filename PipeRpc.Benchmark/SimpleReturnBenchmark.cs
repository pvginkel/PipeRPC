using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BenchmarkDotNet.Attributes;

namespace PipeRpc.Benchmark
{
    [SimpleJob(warmupCount: 5, targetCount: 5)]
    public class SimpleReturnBenchmark : BenchmarkBase
    {
        [Benchmark]
        public int ReturnInt()
        {
            return Server.Invoke<int>("ReturnInt", 42);
        }
    }
}
