using System;
using Awaken.TG.MVC.Attributes;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using UnityEngine;

namespace Awaken.TG.Main.Saving.CustomSerializers {
    [UnityEngine.Scripting.Preserve]
    public class Vector4Converter : JsonConverter {
        public override void WriteJson(JsonWriter writer, object v, JsonSerializer serializer) {
            Vector4 value = (Vector4)v;
            JObject j = new JObject {{"x", value.x}, {"y", value.y}, {"z", value.z}, {"w", value.w}};
            j.WriteTo(writer);
        }

        //CanRead is false which means the default implementation will be used instead.
        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer) {
            return existingValue;
        }

        public override bool CanWrite => true;
        public override bool CanRead => false;
        
        public override bool CanConvert(Type objectType) {
            return objectType == typeof(Vector4);
        }
    }
}