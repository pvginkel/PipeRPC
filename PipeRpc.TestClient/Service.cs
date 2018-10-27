using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PipeRpc.TestClient
{
    public class Service
    {
        public int ReturnInt(int value)
        {
            return value;
        }

        public ComplexObject ReturnComplexObject(ComplexObject value)
        {
            return value;
        }
    }
}
