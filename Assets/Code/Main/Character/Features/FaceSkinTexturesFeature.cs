using Awaken.TG.Main.Character.Features.Config;
using Awaken.TG.Main.Heroes.CharacterCreators.Data;
using Awaken.Utility;
using Newtonsoft.Json;

namespace Awaken.TG.Main.Character.Features {
    public partial class FaceSkinTexturesFeature : SkinTexturesFeature<FaceSkinTexturesFeature> {
        public override ushort TypeForSerialization => SavedTypes.FaceSkinTexturesFeature;

        protected override RendererMarkerMaterialType TargetMaterialType => RendererMarkerMaterialType.Face;

        [JsonConstructor, UnityEngine.Scripting.Preserve] public FaceSkinTexturesFeature() { }

        public FaceSkinTexturesFeature(in BodyConfig bodyConfig) {
            _albedoReference = bodyConfig.albedo.Get();
            _normalTeReference = bodyConfig.normal.Get();
            _maskReference = bodyConfig.mask.Get();
        }
        
        public FaceSkinTexturesFeature(in CharacterSkinConfig skinConfig) {
            _albedoReference = skinConfig.Albedo.Get();
            _normalTeReference = skinConfig.Normal.Get();
            _maskReference = skinConfig.Mask.Get();
        }
    }
}