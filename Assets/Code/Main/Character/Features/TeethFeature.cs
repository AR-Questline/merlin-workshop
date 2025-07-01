using Awaken.TG.Assets;
using Awaken.Utility;
using Newtonsoft.Json;
using UnityEngine;

namespace Awaken.TG.Main.Character.Features {
    public sealed partial class TeethFeature : MainTextureMaterialBasedBodyFeature<TeethFeature> {
        public override ushort TypeForSerialization => SavedTypes.TeethFeature;

        static readonly int MainTextureID = Shader.PropertyToID("_BaseColorMap");

        protected override RendererMarkerMaterialType TargetMaterialType => RendererMarkerMaterialType.Teeth;

        protected override int MainTextureShaderID => MainTextureID;

        [JsonConstructor, UnityEngine.Scripting.Preserve] public TeethFeature() : base() {}
        public TeethFeature(ARAssetReference texture) : base(texture) {}
    }
}
