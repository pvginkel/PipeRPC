using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BenchmarkDotNet.Attributes;
using PipeRpc.Test;
using PipeRpc.TestClient;

namespace PipeRpc.Benchmark
{
    [SimpleJob(invocationCount: 10_000)]
    public class Benchmark
    {
        private RpcServer _server;
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

        [GlobalSetup]
        public void Setup()
        {
            _server = new RpcServer();
        }

        [GlobalCleanup]
        public void Cleanup()
        {
            _server.Dispose();
            _server = null;
        }

        [Benchmark]
        public int ReturnInt()
        {
            return _server.ReturnInt(42);
        }

        [Benchmark]
        public ComplexObject ReturnComplexObject()
        {
            return _server.ReturnComplexObject(_complexObject);
        }
    }
}
