using System;
using System.Linq;
using System.Text;
using Awaken.Utility.Serialization;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Awaken.TG.Main.Saving.CustomSerializers {
    public class UnicodeStringConverter : JsonConverter {
        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer) {
            UnicodeString uni = value as UnicodeString;
            string uniText = uni;
            if (uniText == null) {
                writer.WriteNull();
                return;
            }
            byte[] bytes = Encoding.Unicode.GetBytes(uniText);
            
            writer.WriteStartArray();
            for (int i = 0; i < bytes.Length; i++) {
                writer.WriteValue(bytes[i]);
            }
            writer.WriteEndArray();
        }
        
        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer) {
            if (reader.TokenType != JsonToken.StartArray) {
                return null;
            }
            
            JArray jArray = JArray.Load(reader);
            byte[] bytes = jArray.Select(t => t.Value<byte>()).ToArray();
            string text = Encoding.Unicode.GetString(bytes);
            return new UnicodeString(text);
        }

        public override bool CanConvert(Type objectType) {
            return objectType == typeof(UnicodeString);
        }
    }
}
