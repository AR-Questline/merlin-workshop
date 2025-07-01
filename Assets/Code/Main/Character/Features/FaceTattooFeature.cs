using Awaken.Utility;
using Cysharp.Threading.Tasks;
using Newtonsoft.Json;
using UnityEngine;

namespace Awaken.TG.Main.Character.Features {
    public partial class FaceTattooFeature : TattooFeature<FaceTattooFeature> {
        public override ushort TypeForSerialization => SavedTypes.FaceTattooFeature;

        protected override RendererMarkerMaterialType TargetMaterialType => RendererMarkerMaterialType.Face;

        [JsonConstructor, UnityEngine.Scripting.Preserve] public FaceTattooFeature() : base() { }

        public FaceTattooFeature(in TattooConfig config) : base(config) { }

        protected override UniTask<Texture> LoadMapTexture() {
            return _config.Face?.LoadAsset<Texture>().ToUniTask() ?? new UniTask<Texture>(null);
        }

        protected override UniTask<Texture> LoadNormalMapTexture() {
            return _config.FaceNormal?.LoadAsset<Texture>().ToUniTask() ?? new UniTask<Texture>(null);
        }
    }
}