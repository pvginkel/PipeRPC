using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using NUnit.Framework;
using PipeRpc.TestClient;

namespace PipeRpc.Test
{
    [TestFixture]
    public class TestFixture
    {
        [Test]
        public void ReturnInt()
        {
            using (var server = new RpcServer())
            {
                Assert.AreEqual(42, server.ReturnInt(42));
            }
        }

        [Test]
        public void ReturnComplexObject()
        {
            var expected = CreateComplexObject();

            ComplexObject actual;

            using (var server = new RpcServer())
            {
                actual = server.ReturnComplexObject(expected);
            }

            Assert.AreEqual(expected.StringList, actual.StringList);
            Assert.AreEqual(expected.IntBoolMap.Count, actual.IntBoolMap.Count);
            Assert.AreEqual(expected.IntBoolMap[42], actual.IntBoolMap[42]);
        }

        [Test]
        public void BulkReturnComplexObject()
        {
            var expected = CreateComplexObject();

            using (var server = new RpcServer())
            {
                for (int i = 0; i < 1_000; i++)
                {
                    server.ReturnComplexObject(expected);
                }
            }
        }

        [Test]
        public void PostBack()
        {
            using (var server = new RpcServer())
            {
                int? postBack = null;
                server.Server.On<int>("PostBack", p => postBack = p);
                server.PostBack(42);
                Assert.AreEqual(42, postBack);
            }
        }

        [Test]
        public void CancelRequest()
        {
            using (var server = new RpcServer())
            using (var tokenSource = new CancellationTokenSource())
            {
                server.Server.On("WaitingForCancel", () => tokenSource.Cancel());
                bool cancelled = server.PostWithCancellationToken(tokenSource.Token);
                Assert.IsTrue(cancelled);
            }
        }

        [Test]
        public void PostException()
        {
            using (var server = new RpcServer())
            {
                // Loop a few times to ensure no internal state breaks because of
                // the thrown exception.
                for (int i = 0; i < 5; i++)
                {
                    try
                    {
                        server.PostException("My Exception");
                    }
                    catch (PipeRpcInvocationException ex)
                    {
                        Assert.AreEqual("My Exception", ex.Message);
                    }
                }
            }
        }

        [Test]
        public void ReturnDateTimes()
        {
            using (var server = new RpcServer())
            {
                var expectedDateTime = DateTime.Now;
                Assert.AreEqual(expectedDateTime, server.ReturnDateTime(expectedDateTime));
                var expectedDateTimeOffset = DateTimeOffset.Now;
                Assert.AreEqual(expectedDateTimeOffset, server.ReturnDateTimeOffset(expectedDateTimeOffset));
            }
        }

        [Test]
        public void CallUnknownMethod()
        {
            try
            {
                using (var server = new RpcServer())
                {
                    server.Server.Invoke("UnknownMethod");
                }
            }
            catch (PipeRpcException ex)
            {
                Assert.AreEqual("Unexpected end of stream", ex.Message);
            }
        }

        private static ComplexObject CreateComplexObject()
        {
            return new ComplexObject
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
        }
    }
}
