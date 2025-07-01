using System;
using Newtonsoft.Json;
using UnityEngine;

namespace Awaken.TG.Main.Saving.CustomSerializers {
    public class Vector3Converter : JsonConverter {
        public static void Write(JsonWriter writer, Vector3 vector) {
            writer.WriteStartObject();
            writer.WritePropertyName("x");
            writer.WriteValue(vector.x);
            writer.WritePropertyName("y");
            writer.WriteValue(vector.y);
            writer.WritePropertyName("z");
            writer.WriteValue(vector.z);
            writer.WriteEndObject();
        }
        
        public override void WriteJson(JsonWriter writer, object v, JsonSerializer serializer) {
            Write(writer, (Vector3)v);
        }

        //CanRead is false which means the default implementation will be used instead.
        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer) {
            return existingValue;
        }

        public override bool CanWrite => true;
        public override bool CanRead => false;
        
        public override bool CanConvert(Type objectType) {
            return objectType == typeof(Vector3);
        }
    }
}