using Awaken.Utility;
using Cysharp.Threading.Tasks;
using Newtonsoft.Json;
using UnityEngine;

namespace Awaken.TG.Main.Character.Features {
    public partial class BodyTattooFeature : TattooFeature<BodyTattooFeature> {
        public override ushort TypeForSerialization => SavedTypes.BodyTattooFeature;

        protected override RendererMarkerMaterialType TargetMaterialType => RendererMarkerMaterialType.Body;

        [JsonConstructor, UnityEngine.Scripting.Preserve] public BodyTattooFeature() : base() { }

        public BodyTattooFeature(in TattooConfig config) : base(config) { }

        protected override UniTask<Texture> LoadMapTexture() {
            return _config.Torso?.LoadAsset<Texture>().ToUniTask() ?? new UniTask<Texture>(null);
        }

        protected override UniTask<Texture> LoadNormalMapTexture() {
            return _config.TorsoNormal?.LoadAsset<Texture>().ToUniTask() ?? new UniTask<Texture>(null);
        }
    }
}