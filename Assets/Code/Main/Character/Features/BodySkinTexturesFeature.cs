using Awaken.TG.Main.Character.Features.Config;
using Awaken.TG.Main.Heroes.CharacterCreators.Data;
using Awaken.Utility;
using Newtonsoft.Json;

namespace Awaken.TG.Main.Character.Features {
    public partial class BodySkinTexturesFeature : SkinTexturesFeature<BodySkinTexturesFeature> {
        public override ushort TypeForSerialization => SavedTypes.BodySkinTexturesFeature;

        protected override RendererMarkerMaterialType TargetMaterialType => RendererMarkerMaterialType.Body;
        
        [JsonConstructor, UnityEngine.Scripting.Preserve] public BodySkinTexturesFeature() { }

        public BodySkinTexturesFeature(in BodyConfig bodyConfig) {
            _albedoReference = bodyConfig.bodyAlbedo.Get();
            _normalTeReference = bodyConfig.bodyNormal.Get();
            _maskReference = bodyConfig.bodyMask.Get();
        }
        
        public BodySkinTexturesFeature(in CharacterSkinConfig skinConfig) {
            _albedoReference = skinConfig.Albedo.Get();
            _normalTeReference = skinConfig.Normal.Get();
            _maskReference = skinConfig.Mask.Get();
        }   
    }
}