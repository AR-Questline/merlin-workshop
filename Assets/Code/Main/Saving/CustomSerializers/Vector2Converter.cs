using System;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using UnityEngine;

namespace Awaken.TG.Main.Saving.CustomSerializers {
    public class Vector2Converter : JsonConverter {
        public override void WriteJson(JsonWriter writer, object v, JsonSerializer serializer) {
            Vector2 value = (Vector2)v;
            JObject j = new JObject {{"x", value.x}, {"y", value.y}};
            j.WriteTo(writer);
        }

        //CanRead is false which means the default implementation will be used instead.
        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer) {
            return existingValue;
        }

        public override bool CanWrite => true;
        public override bool CanRead => false;
        
        public override bool CanConvert(Type objectType) {
            return objectType == typeof(Vector2);
        }
    }
}