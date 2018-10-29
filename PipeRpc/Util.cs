using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PipeRpc
{
    internal static class Util
    {
        [DebuggerNonUserCode]
        [DebuggerStepThrough]
        public static void DisposeSilently(IDisposable disposable)
        {
            try
            {
                disposable?.Dispose();
            }
            catch
            {
                // Ignore exceptions.
            }
        }

        [DebuggerNonUserCode]
        [DebuggerStepThrough]
        public static void NoThrow(Action action)
        {
            try
            {
                action();
            }
            catch
            {
                // Ignore exceptions.
            }
        }
    }
}
