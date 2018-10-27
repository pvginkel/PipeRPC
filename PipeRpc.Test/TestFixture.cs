using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
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
