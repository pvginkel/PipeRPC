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
    [TestFixture(PipeRpcServerMode.Local)]
    [TestFixture(PipeRpcServerMode.Remote)]
    public class TestFixture : FixtureBase
    {
        public TestFixture(PipeRpcServerMode mode)
            : base(mode)
        {
        }

        [Test]
        public void ReturnInt()
        {
            using (var server = CreateServer())
            {
                Assert.AreEqual(42, server.Invoke<int>("ReturnInt", 42));
            }
        }

        [Test]
        public void ReturnComplexObject()
        {
            var expected = CreateComplexObject();

            ComplexObject actual;

            using (var server = CreateServer())
            {
                actual = server.Invoke<ComplexObject>("ReturnComplexObject", expected);
            }

            Assert.AreEqual(expected.StringList, actual.StringList);
            Assert.AreEqual(expected.IntBoolMap.Count, actual.IntBoolMap.Count);
            Assert.AreEqual(expected.IntBoolMap[42], actual.IntBoolMap[42]);
        }

        [Test]
        public void BulkReturnComplexObject()
        {
            var expected = CreateComplexObject();

            using (var server = CreateServer())
            {
                for (int i = 0; i < 1_000; i++)
                {
                    server.Invoke<ComplexObject>("ReturnComplexObject", expected);
                }
            }
        }

        [Test]
        public void PostBack()
        {
            using (var server = CreateServer())
            {
                int? postBack = null;
                server.On<int>("PostBack", p => postBack = p);
                server.Invoke("PostBack", 42);
                Assert.AreEqual(42, postBack);
            }
        }

        [Test]
        public void CancelRequest()
        {
            using (var server = CreateServer())
            using (var tokenSource = new CancellationTokenSource())
            {
                server.On("WaitingForCancel", () => tokenSource.Cancel());
                bool cancelled = server.Invoke<bool>("PostWithCancellationToken", tokenSource.Token);
                Assert.IsTrue(cancelled);
            }
        }

        [Test]
        public void PostException()
        {
            using (var server = CreateServer())
            {
                // Loop a few times to ensure no internal state breaks because of
                // the thrown exception.
                for (int i = 0; i < 5; i++)
                {
                    try
                    {
                        server.Invoke("PostException", "My Exception");
                    }
                    catch (PipeRpcInvocationException ex)
                    {
                        Assert.AreEqual("My Exception", ex.Message);
                        continue;
                    }

                    Assert.Fail();
                }
            }
        }

        [Test]
        public void ReturnDateTimes()
        {
            using (var server = CreateServer())
            {
                var expectedDateTime = DateTime.Now;
                Assert.AreEqual(expectedDateTime, server.Invoke<DateTime>("ReturnDateTime", expectedDateTime));
                var expectedDateTimeOffset = DateTimeOffset.Now;
                Assert.AreEqual(expectedDateTimeOffset, server.Invoke<DateTimeOffset>("ReturnDateTimeOffset", expectedDateTimeOffset));
            }
        }

        [Test]
        public void CallUnknownMethod()
        {
            try
            {
                using (var server = CreateServer())
                {
                    server.Invoke("UnknownMethod");
                }
            }
            catch (PipeRpcException ex)
            {
                if (Mode == PipeRpcServerMode.Local)
                    Assert.AreEqual("Method 'UnknownMethod' not found", ex.Message);
                else
                    Assert.AreEqual("Unexpected end of stream", ex.Message);
                return;
            }

            Assert.Fail();
        }

        [Test]
        public void InvokeBack()
        {
            using (var server = CreateServer())
            {
                server.On<int, int, int>("Multiply", (a, b) => a * b);
                int result = server.Invoke<int>("InvokeBackWithInt", 3, 4);
                Assert.AreEqual((3 + 1) * (4 + 2), result);
            }
        }

        [Test]
        public void InvokeBackThrowsException()
        {
            using (var server = CreateServer())
            {
                server.On<int, int, int>("Multiply", (a, b) => throw new NotSupportedException("Not supported test"));
                Exception exception = null;

                try
                {
                    server.Invoke<int>("InvokeBackWithInt", 3, 4);
                }
                catch (Exception ex)
                {
                    exception = ex;
                }

                Assert.IsNotNull(exception);
                Assert.IsAssignableFrom<PipeRpcInvocationException>(exception);
                Assert.AreEqual("Not supported test", exception.Message);
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
