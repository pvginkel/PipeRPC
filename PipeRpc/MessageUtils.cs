using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace PipeRpc
{
    internal static class MessageUtils
    {
        public static object ParseResult(JsonReader reader, Type resultType, JsonSerializer serializer)
        {
            JsonUtil.ReadForType(reader, resultType);
            return serializer.Deserialize(reader, resultType);
        }

        public static void SendResult(JsonWriter writer, object result, bool haveResult, JsonSerializer serializer)
        {
            writer.WriteStartArray();
            writer.WriteValue("result");
            if (haveResult)
                serializer.Serialize(writer, result);
            writer.WriteEndArray();
            writer.Flush();
        }

        public static Exception ParseException(JsonReader reader)
        {
            string message = reader.ReadAsString();
            string type = reader.ReadAsString();
            string stackTrace = reader.ReadAsString();

            return new PipeRpcInvocationException(message, type, stackTrace);
        }

        public static void SendException(JsonWriter writer, Exception exception)
        {
            if (exception is TargetInvocationException invocationException && invocationException.InnerException != null)
                exception = invocationException.InnerException;

            writer.WriteStartArray();
            writer.WriteValue("exception");
            writer.WriteValue(exception.Message);
            writer.WriteValue(exception.GetType().FullName);
            writer.WriteValue(exception.StackTrace);
            writer.WriteEndArray();
            writer.Flush();
        }
    }
}
