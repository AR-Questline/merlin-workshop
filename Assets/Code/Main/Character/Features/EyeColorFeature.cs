using Awaken.Kandra;
using Awaken.TG.Utility.Attributes;
using Awaken.Utility;
using Newtonsoft.Json;
using UnityEngine;

namespace Awaken.TG.Main.Character.Features {
    public sealed partial class EyeColorFeature : MaterialBasedBodyFeature {
        public override ushort TypeForSerialization => SavedTypes.EyeColorFeature;

        public static readonly int TintID = Shader.PropertyToID("_Iris_Tint");

        [Saved] Color _color;

        public Color Color => _color;

        protected override RendererMarkerMaterialType TargetMaterialType => RendererMarkerMaterialType.Eyes;

        [JsonConstructor, UnityEngine.Scripting.Preserve] EyeColorFeature() { }
        public EyeColorFeature(Color color) {
            _color = color;
        }

        public EyeColorFeature Copy() => new EyeColorFeature(Color);
        public override BodyFeature GenericCopy() => Copy();

        protected override void ApplyModifications(Material material, KandraRenderer renderer) {
            material.SetColor(TintID, Color);
        }

        protected override void CleanupModification(Material material, KandraRenderer renderer) {
            material.SetColor(TintID, Color.white);
        }

        protected override void FinalizeCleanup() { }
    }
}