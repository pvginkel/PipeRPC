using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace PipeRpc
{
    [DebuggerDisplay("Name = {Method.Name}")]
    internal class ServiceMethod
    {
        public static ServiceMethod FromMethodInfo(MethodInfo method)
        {
            var parameterTypes = new List<Type>();
            foreach (var parameter in method.GetParameters())
            {
                parameterTypes.Add(parameter.ParameterType);
            }

            return new ServiceMethod(
                method,
                method.ReturnType,
                new ReadOnlyCollection<Type>(parameterTypes)
            );
        }

        public MethodInfo Method { get; }
        public Type ReturnType { get; }
        public IList<Type> ParameterTypes { get; }

        private ServiceMethod(MethodInfo method, Type returnType, IList<Type> parameterTypes)
        {
            Method = method;
            ReturnType = returnType;
            ParameterTypes = parameterTypes;
        }
    }
}
