using System;
using Awaken.TG.Assets;
using Awaken.TG.MVC.Attributes;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Awaken.TG.Main.Saving.CustomSerializers {
    public class AssetReferenceConverter : JsonConverter {
        public static void Write(JsonWriter writer, ShareableARAssetReference value) {
            writer.WriteStartObject();
            if (!string.IsNullOrWhiteSpace(value?.AssetGUID)) {
                writer.WritePropertyName("guid");
                writer.WriteValue(value.AssetGUID);
            }
            if (!string.IsNullOrWhiteSpace(value?.SubObject)) {
                writer.WritePropertyName("subObjectName");
                writer.WriteValue(value.SubObject);
            }
            writer.WriteEndObject();
        }
        
        public static void Write(JsonWriter writer, ARAssetReference value) {
            writer.WriteStartObject();
            if (!string.IsNullOrWhiteSpace(value?.Address)) {
                writer.WritePropertyName("guid");
                writer.WriteValue(value.Address);
            }
            if (!string.IsNullOrWhiteSpace(value?.SubObjectName)) {
                writer.WritePropertyName("subObjectName");
                writer.WriteValue(value.SubObjectName);
            }
            writer.WriteEndObject();
        }
        
        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer) {
            if (value is ARAssetReference arAssetReference) {
                Write(writer, arAssetReference);
            } else if (value is ShareableARAssetReference shareableARAssetReference) {
                Write(writer, shareableARAssetReference);
            } else {
                Write(writer, (ARAssetReference)null);
            }
        }

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer) {
            try {
                JObject jObject = JObject.Load(reader);
                var result = new ARAssetReference((string) jObject["guid"], (string) jObject["subObjectName"]);
                if (objectType == typeof(ARAssetReference)) {
                    return result;
                } else {
                    return new ShareableARAssetReference(result);
                }
            } catch {
                return null;
            }
        }

        public override bool CanConvert(Type objectType) {
            return typeof(ARAssetReference) == objectType ||
                   typeof(ShareableARAssetReference) == objectType;
        }
    }
}