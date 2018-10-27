using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace PipeRpc
{
    internal static class JsonUtil
    {
        public static void ReadStartArray(JsonReader reader)
        {
            ReadTokenType(reader, JsonToken.StartArray);
        }

        public static void ReadEndArray(JsonReader reader)
        {
            ReadTokenType(reader, JsonToken.EndArray);
        }

        private static void ReadTokenType(JsonReader reader, JsonToken tokenType)
        {
            Read(reader);
            ExpectTokenType(reader, tokenType);
        }

        public static void ExpectTokenType(JsonReader reader, JsonToken tokenType)
        {
            if (reader.TokenType != tokenType)
                throw new PipeRpcException($"Unexpected {reader.TokenType}, expected {tokenType}");
        }

        public static void Read(JsonReader reader)
        {
            if (!reader.Read())
                throw new PipeRpcException("Unexpected end of stream");
        }
    }
}
