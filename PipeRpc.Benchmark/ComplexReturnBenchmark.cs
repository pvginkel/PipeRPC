using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BenchmarkDotNet.Attributes;
using PipeRpc.TestClient;

namespace PipeRpc.Benchmark
{
    // For some reason, ReturnComplexObject causes just one op to be
    // run per benchmark.
    [SimpleJob(warmupCount: 5, targetCount: 5, invocationCount: 10_000)]
    public class ComplexReturnBenchmark : BenchmarkBase
    {
        private readonly ComplexObject _complexObject = new ComplexObject
        {
            StringList =
            {
                "a",
                "b"
            },
            IntBoolMap =
            {
                { 42, true },
                { -42, false }
            }
        };

        [Benchmark]
        public ComplexObject ReturnComplexObject()
        {
            return Server.Invoke<ComplexObject>("ReturnComplexObject", _complexObject);
        }
    }
}
