using System;
using Awaken.TG.MVC.Attributes;
using Awaken.Utility.Enums;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Awaken.TG.Main.Saving.CustomSerializers {
    public class RichEnumConverter : JsonConverter {
        public static void Write(JsonWriter writer, RichEnum value) {
            writer.WriteValue(value.Serialize());
        }
        
        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer) {
            RichEnum richEnum = (RichEnum)value;
            Write(writer, richEnum);
        }

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer) {
            try {
                JToken token = JToken.Load(reader);
                string value = token.ToObject<string>();
                return RichEnum.Deserialize(value);
            } catch {
                return null;
            }
        }

        public override bool CanConvert(Type objectType) {
            return AttributesCache.GetIsAssignableFrom(typeof(RichEnum), objectType);
        }
    }
}