using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
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

        public void PostBack(int value, IOperationContext context)
        {
            context.Post("PostBack", value);
        }

        public bool PostWithCancellationToken(IOperationContext context, CancellationToken token)
        {
            using (var @event = new ManualResetEventSlim())
            {
                token.Register(() => @event.Set());
                context.Post("WaitingForCancel");
                return @event.Wait(TimeSpan.FromSeconds(1));
            }
        }

        public void PostException(string message)
        {
            throw new Exception(message);
        }

        public DateTime ReturnDateTime(DateTime dateTime)
        {
            return dateTime;
        }

        public DateTimeOffset ReturnDateTimeOffset(DateTimeOffset dateTimeOffset)
        {
            return dateTimeOffset;
        }
    }
}
