using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using PipeRpc.TestClient;

namespace PipeRpc.Test
{
    public class FixtureBase
    {
        protected PipeRpcServerMode Mode { get; }

        public FixtureBase(PipeRpcServerMode mode)
        {
            Mode = mode;
        }

        protected PipeRpcServer CreateServer(params string[] args)
        {
            var server = new PipeRpcServer(Mode);
            args = new[] { server.Handle.ToString() }.Concat(args).ToArray();
            server.Start(new ClientStartInfo(typeof(Program).Assembly.Location, null, args));
            return server;
        }
    }
}
