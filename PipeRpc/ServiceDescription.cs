using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace PipeRpc
{
    internal class ServiceDescription
    {
        public static ServiceDescription FromType(Type type)
        {
            var methods = new Dictionary<string, ServiceMethod>();

            foreach (var method in type.GetMethods(BindingFlags.Public | BindingFlags.Instance))
            {
                if (method.IsSpecialName)
                    continue;

                if (methods.ContainsKey(method.Name))
                    throw new PipeRpcException("Pipe RPC does not support method overloading");

                methods.Add(method.Name, ServiceMethod.FromMethodInfo(method));
            }

            return new ServiceDescription(methods);
        }

        private readonly Dictionary<string, ServiceMethod> _methods;

        private ServiceDescription(Dictionary<string, ServiceMethod> methods)
        {
            _methods = methods;
        }

        public ServiceMethod GetMethod(string name)
        {
            if (!_methods.TryGetValue(name, out var method))
                throw new PipeRpcException($"Method '{name}' not found");
            return method;
        }
    }
}
