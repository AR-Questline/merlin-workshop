using Awaken.Utility;
using System;
using Awaken.TG.Main.Saving;
using Awaken.TG.Utility.Attributes;
using Newtonsoft.Json;
using UnityEngine;

namespace Awaken.TG.Main.Character.Features {
    [Serializable]
    public partial struct BlendShape {
        public ushort TypeForSerialization => SavedTypes.BlendShape;

        [SerializeField, Saved] public string name;
        [SerializeField, Saved(0f)] public float weight;
        
        public BlendShape(string name, float weight) {
            this.name = name;
            this.weight = weight;
        }

        public void WriteSavables(JsonWriter jsonWriter, JsonSerializer serializer) {
            jsonWriter.WriteStartObject();
            JsonUtils.JsonWrite(jsonWriter, serializer, nameof(name), name);
            JsonUtils.JsonWrite(jsonWriter, serializer, nameof(weight), weight);
            jsonWriter.WriteEndObject();
        }
    }
}
