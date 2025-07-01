using System;
using Awaken.TG.MVC.Attributes;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using UnityEngine;

namespace Awaken.TG.Main.Saving.CustomSerializers {
    public class ColorConverter : JsonConverter {
        public static void Write(JsonWriter writer, Color value) {
            writer.WriteStartObject();
            writer.WritePropertyName("r");
            writer.WriteValue(value.r);
            writer.WritePropertyName("g");
            writer.WriteValue(value.g);
            writer.WritePropertyName("b");
            writer.WriteValue(value.b);
            writer.WritePropertyName("a");
            writer.WriteValue(value.a);
            writer.WriteEndObject();
        }

        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer) {
            Write(writer, (Color)value);
        }

        //CanRead is false which means the default implementation will be used instead.
        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer) {
            try {
                JToken token = JToken.Load(reader);
                Color value = token.ToObject<Color>();
                return value;
            } catch {
                return Color.clear;
            }
        }
        
        public override bool CanWrite => true;
        public override bool CanRead => true;

        public override bool CanConvert(Type objectType) {
            return objectType == typeof(Color);
        }
    }
}