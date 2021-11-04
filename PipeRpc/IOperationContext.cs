using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PipeRpc
{
    public interface IOperationContext
    {
        void Post(string @event, params object[] args);
        void Invoke(string @event, params object[] args);
        T Invoke<T>(string @event, params object[] args);
    }
}
