using Awaken.Kandra;
using Awaken.TG.Assets;
using Awaken.TG.Utility.Attributes;
using Awaken.Utility;
using Cysharp.Threading.Tasks;
using Newtonsoft.Json;
using UnityEngine;

namespace Awaken.TG.Main.Character.Features {
    public sealed partial class EyebrowFeature : MaterialBasedBodyFeature {
        public override ushort TypeForSerialization => SavedTypes.EyebrowFeature;

        static readonly int MainTextureID = Shader.PropertyToID("_DiffuseMap");
        
        [Saved] ARAssetReference _textureReference;

        Texture _instance;

        protected override RendererMarkerMaterialType TargetMaterialType => RendererMarkerMaterialType.Eyebrows;

        [JsonConstructor, UnityEngine.Scripting.Preserve] public EyebrowFeature() {}

        public EyebrowFeature(ARAssetReference texture) {
            _textureReference = texture;
        }
        
        protected override async UniTask Initialize() {
            _instance = await _textureReference.LoadAsset<Texture>().ToUniTask();
        }

        protected override void ApplyModifications(Material material, KandraRenderer renderer) {
            material.SetTexture(MainTextureID, _instance);
            Features.ApplyHairConfig(material, true);
            renderer.TexturesChanged();
        }

        protected override void CleanupModification(Material material, KandraRenderer renderer) {
            material.SetTexture(MainTextureID, null);
            renderer.TexturesChanged();
        }

        protected override void FinalizeCleanup() {
            _textureReference.ReleaseAsset();
            _instance = null;
        }

        public override BodyFeature GenericCopy() => Copy();

        public EyebrowFeature Copy() {
            var copy = new EyebrowFeature {
                _textureReference = _textureReference.DeepCopy(),
            };
            return copy;
        }
    }
}