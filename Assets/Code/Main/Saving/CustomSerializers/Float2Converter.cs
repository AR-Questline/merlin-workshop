using System;
using Newtonsoft.Json;
using Unity.Mathematics;

namespace Awaken.TG.Main.Saving.CustomSerializers {
    public class Float2Converter : JsonConverter {
        public override bool CanWrite => true;
        public override bool CanRead => true;
        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer) {
            var point = (float2)value;
            writer.WriteStartArray();
            writer.WriteValue(point.x);
            writer.WriteValue(point.y);
            writer.WriteEndArray();
        }

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer) {
            if (reader.TokenType != JsonToken.StartArray)
                throw new JsonSerializationException("Expected start of array");

            reader.Read();
            float x = Convert.ToSingle(reader.Value);
            reader.Read();
            float y = Convert.ToSingle(reader.Value);

            reader.Read(); // Move past end of array
            if (reader.TokenType != JsonToken.EndArray)
                throw new JsonSerializationException("Expected end of array");

            return new float2(x, y);
        }

        public override bool CanConvert(Type objectType) {
            return objectType == typeof(float2);
        }
    }
}