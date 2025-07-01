using System;
using Awaken.TG.Main.Templates;
using Awaken.TG.MVC.Attributes;
using Awaken.Utility.Debugging;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Awaken.TG.Main.Saving.CustomSerializers {
    public class TemplateConverter : JsonConverter {
        public static void Write(JsonWriter writer, ITemplate value) {
            if (string.IsNullOrWhiteSpace(value.GUID)) {
                Log.Important?.Warning($"Cannot serialize template {value}, since it doesn't have GUID set - will write null instead.");
            }
            writer.WriteStartObject();
            writer.WritePropertyName("guid");
            writer.WriteValue(value.GUID);
            writer.WriteEndObject();
        }

        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer) {
            Write(writer, (ITemplate)value);
        }

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue,
            JsonSerializer serializer) {

            // If null was saved then attempt to read JObject will throw, so bail out faster
            if (reader.TokenType == JsonToken.Null) {
                return null;
            }
            
            JObject jObject = JObject.Load(reader);
            return Read(jObject);
        }

        public static object Read(JObject jObject) {
            jObject.TryGetValue("guid", out var token);
            string serializedGuid = token!.Value<string>();
            return TemplatesUtil.Load<UnityEngine.Object>(serializedGuid);
        }

        public override bool CanConvert(Type objectType) {
            return AttributesCache.GetIsAssignableFrom(typeof(ITemplate), objectType);
        }
    }
}