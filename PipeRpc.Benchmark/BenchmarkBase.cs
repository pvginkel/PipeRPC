using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using BenchmarkDotNet.Attributes;
using PipeRpc.TestClient;

namespace PipeRpc.Benchmark
{
    public abstract class BenchmarkBase
    {
        [Params(PipeRpcServerMode.Local, PipeRpcServerMode.Remote)]
        public PipeRpcServerMode Mode { get; set; }

        protected PipeRpcServer Server { get; private set; }

        [GlobalSetup]
        public virtual void Setup()
        {
            Server = new PipeRpcServer(Mode);
            Server.Start(new ClientStartInfo(typeof(TestClient.Program).Assembly.Location, null, Server.Handle.ToString()));
        }

        [GlobalCleanup]
        public virtual void Cleanup()
        {
            Server.Dispose();
            Server = null;
        }
    }
}
