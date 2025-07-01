using Awaken.Kandra;
using Awaken.TG.Assets;
using Awaken.TG.Main.Heroes.CharacterCreators.Data;
using Awaken.TG.Utility.Attributes;
using Awaken.Utility;
using Cysharp.Threading.Tasks;
using Newtonsoft.Json;
using UnityEngine;

namespace Awaken.TG.Main.Character.Features {
    public partial class BodyNormalFeature : MaterialBasedBodyFeature {
        public override ushort TypeForSerialization => SavedTypes.BodyNormalFeature;

        static readonly int BodyNormalsMaterialID = Shader.PropertyToID("_NormalMap");
        static readonly int BodyNormalsStrengthID = Shader.PropertyToID("_Normal_Strength");
        
        [Saved] ARAssetReference _textureReference;
        [Saved] float _bodyNormalStrength;
        Texture _instance;

        protected override RendererMarkerMaterialType TargetMaterialType => RendererMarkerMaterialType.Body;

        [JsonConstructor, UnityEngine.Scripting.Preserve] BodyNormalFeature() { }

        public BodyNormalFeature(in CharacterBodyNormal bodyNormal) {
            _bodyNormalStrength = bodyNormal.normalStrength;
            _textureReference = bodyNormal.bodyNormal.DeepCopy();
        }

        public BodyNormalFeature Copy() => new() {
            _textureReference = _textureReference.DeepCopy(),
            _bodyNormalStrength = _bodyNormalStrength
        };

        public override BodyFeature GenericCopy() => Copy();

        protected override async UniTask Initialize() {
            _instance = await _textureReference.LoadAsset<Texture>().ToUniTask();
        }

        protected override void ApplyModifications(Material material, KandraRenderer renderer) {
            material.SetTexture(BodyNormalsMaterialID, _instance);
            material.SetFloat(BodyNormalsStrengthID, _bodyNormalStrength);
            renderer.TexturesChanged();
        }

        protected override void CleanupModification(Material material, KandraRenderer renderer) {
            material.SetTexture(BodyNormalsMaterialID, null);
            material.SetFloat(BodyNormalsStrengthID, 0);
            renderer.TexturesChanged();
        }

        protected override void FinalizeCleanup() {
            _textureReference.ReleaseAsset();
            _instance = null;
        }
    }
}