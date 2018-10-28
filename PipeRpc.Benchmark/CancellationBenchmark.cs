using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using BenchmarkDotNet.Attributes;
using PipeRpc.Test;
using PipeRpc.TestClient;

namespace PipeRpc.Benchmark
{
    [SimpleJob(warmupCount: 5, targetCount: 5)]
    public class CancellationBenchmark : BenchmarkBase
    {
        private Action _cancelAction;

        public override void Setup()
        {
            base.Setup();
            Server.On("WaitingForCancel", () => _cancelAction?.Invoke());
        }

        [Benchmark]
        public bool Cancellation()
        {
            var tokenSource = new CancellationTokenSource();
            try
            {
                _cancelAction = () => tokenSource.Cancel();
                return Server.Invoke<bool>("PostWithCancellationToken", tokenSource.Token);
            }
            finally
            {
                _cancelAction = null;
            }
        }
    }
}
