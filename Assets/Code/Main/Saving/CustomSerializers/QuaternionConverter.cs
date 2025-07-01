using System;
using Awaken.TG.MVC.Attributes;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using UnityEngine;

namespace Awaken.TG.Main.Saving.CustomSerializers {
    public class QuaternionConverter : JsonConverter {
        public static void Write(JsonWriter writer, Quaternion value) {
            writer.WriteStartObject();
            writer.WritePropertyName("x");
            writer.WriteValue(value.x);
            writer.WritePropertyName("y");
            writer.WriteValue(value.y);
            writer.WritePropertyName("z");
            writer.WriteValue(value.z);
            writer.WritePropertyName("w");
            writer.WriteValue(value.w);
            writer.WriteEndObject();
        }
        
        public override void WriteJson(JsonWriter writer, object v, JsonSerializer serializer) {
            Write(writer, (Quaternion)v);
        }

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer) {
            try {
                JToken token = JToken.Load(reader);
                Quaternion value = token.ToObject<Quaternion>();
                return value;
            } catch {
                return Quaternion.identity;
            }
        }

        public override bool CanWrite => true;
        public override bool CanRead => true;

        public override bool CanConvert(Type objectType) {
            return objectType == typeof(Quaternion);
        }
    }
}