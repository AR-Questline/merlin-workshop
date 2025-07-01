using Awaken.Kandra;
using Awaken.TG.Utility.Attributes;
using Awaken.Utility;
using Newtonsoft.Json;
using UnityEngine;

namespace Awaken.TG.Main.Character.Features {
    public sealed partial class SkinColorFeature : MaterialBasedBodyFeature {
        public override ushort TypeForSerialization => SavedTypes.SkinColorFeature;

        public static readonly int TintID = Shader.PropertyToID("_DiffuseColor");

        [Saved] Color _color;

        public Color Color => _color;

        protected override RendererMarkerMaterialType TargetMaterialType => RendererMarkerMaterialType.Skin;

        [JsonConstructor, UnityEngine.Scripting.Preserve] SkinColorFeature() { }
        public SkinColorFeature(Color color) {
            _color = color;
        }

        public SkinColorFeature Copy() => new SkinColorFeature(Color);
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