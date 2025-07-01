using System;
using Awaken.TG.MVC.Attributes;
using Newtonsoft.Json;
using UnityEngine;

namespace Awaken.TG.Main.Saving.CustomSerializers {
    public class TextureConverter : JsonConverter {
        public override void WriteJson(JsonWriter writer, object v, JsonSerializer serializer) {
            Texture2D value = (Texture2D)v;
            writer.WriteValue(value.EncodeToPNG());
        }

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer) {
            var str = reader.Value as string;
            if (string.IsNullOrWhiteSpace(str)) {
                return null;
            }
            var bytes = Convert.FromBase64String(str);

            var texture = new Texture2D(1, 1);
            texture.LoadImage(bytes);
            texture.Apply(false, true);

            return texture;
        }
        
        public override bool CanConvert(Type objectType) {
            return AttributesCache.GetIsAssignableFrom(typeof(Texture2D), objectType);
        }
    }
}