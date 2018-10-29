using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NUnit.Framework;

namespace PipeRpc.Test
{
    [TestFixture(PipeRpcServerMode.Remote)]
    public class RemoteFixture : FixtureBase
    {
        public RemoteFixture(PipeRpcServerMode mode)
            : base(mode)
        {
        }

        [Test]
        public void ClientDies()
        {
            try
            {
                using (var server = CreateServer())
                {
                    server.Invoke("KillClient");
                }
            }
            catch (PipeRpcException ex)
            {
                Assert.AreEqual("Unexpected end of stream", ex.Message);
                return;
            }

            Assert.Fail();
        }

        [Test]
        public void ClientDoesntStart()
        {
            try
            {
                using (var server = CreateServer("0"))
                {
                    server.Invoke<int>("ReturnInt", 42);
                }
            }
            catch (ObjectDisposedException)
            {
                return;
            }

            Assert.Fail();
        }
    }
}
