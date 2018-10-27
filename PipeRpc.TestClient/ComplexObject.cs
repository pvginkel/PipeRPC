using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PipeRpc.TestClient
{
    public class ComplexObject
    {
        public int? OptionalInt { get; set; }
        public List<string> StringList { get; } = new List<string>();
        public Dictionary<int, bool> IntBoolMap { get; } = new Dictionary<int, bool>();
    }
}
