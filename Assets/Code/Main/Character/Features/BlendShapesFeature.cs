using Awaken.Utility;
using System;
using Awaken.Kandra;
using Awaken.TG.Utility.Attributes;
using Awaken.Utility.Debugging;
using Cysharp.Threading.Tasks;
using Newtonsoft.Json;

namespace Awaken.TG.Main.Character.Features {
    public sealed partial class BlendShapesFeature : BodyFeature {
        public override ushort TypeForSerialization => SavedTypes.BlendShapesFeature;

        [Saved] BlendShape[] _shapes;

        KandraRenderer[] _appliedRenderers;

        public bool IsEmpty {
            get {
                foreach (var shape in _shapes) {
                    if (shape.weight > 0) {
                        return false;
                    }
                }
                return true;
            }
        }

        [JsonConstructor, UnityEngine.Scripting.Preserve] BlendShapesFeature() { }
        public BlendShapesFeature(BlendShape[] shapes) {
            _shapes = shapes;
        }

        public override UniTask Spawn() {
            _appliedRenderers = Features.GameObject.GetComponentsInChildren<KandraRenderer>(true);
            foreach (var renderer in _appliedRenderers) {
                BlendShapeUtils.ApplyShapes(renderer, _shapes);
            }

            return UniTask.CompletedTask;
        }
        public override UniTask Release(bool _ = false) {
            foreach (var renderer in _appliedRenderers) {
                BlendShapeUtils.RemoveShapes(renderer, _shapes);
            }
            _appliedRenderers = null;
            return UniTask.CompletedTask;
        }

        public BlendShapesFeature Copy() {
            return new BlendShapesFeature(_shapes);
        }
        public override BodyFeature GenericCopy() => Copy();
        
        public void WriteSavables(JsonWriter jsonWriter, JsonSerializer serializer) {
            jsonWriter.WriteStartObject();
            jsonWriter.WritePropertyName(nameof(_shapes));
            jsonWriter.WriteStartArray();
            var shapes = _shapes ?? Array.Empty<BlendShape>();
            for (int i = 0; i < shapes.Length; i++) {
                if (shapes[i].weight > 0) {
                    shapes[i].WriteSavables(jsonWriter, serializer);
                }
            }
            jsonWriter.WriteEndArray();
            jsonWriter.WriteEndObject();
        }
    }
}