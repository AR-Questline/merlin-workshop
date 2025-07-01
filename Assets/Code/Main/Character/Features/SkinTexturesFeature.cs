using Awaken.Kandra;
using Awaken.TG.Assets;
using Awaken.TG.Utility.Attributes;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace Awaken.TG.Main.Character.Features {
    public abstract partial class SkinTexturesFeature : MaterialBasedBodyFeature {
        protected static readonly int AlbedoID = Shader.PropertyToID("_DiffuseMap");
        protected static readonly int NormalID = Shader.PropertyToID("_NormalMap");
        protected static readonly int MaskID = Shader.PropertyToID("_MaskMap");

        [Saved] protected ARAssetReference _albedoReference;
        [Saved] protected ARAssetReference _normalTeReference;
        [Saved] protected ARAssetReference _maskReference;

        Texture _albedoInstance;
        Texture _normalInstance;
        Texture _maskInstance;

        protected override async UniTask Initialize() {
            var albedoTask = _albedoReference?.LoadAsset<Texture>().ToUniTask() ?? new UniTask<Texture>(null);
            var normalTask = _normalTeReference?.LoadAsset<Texture>().ToUniTask() ?? new UniTask<Texture>(null);
            var maskTask = _maskReference?.LoadAsset<Texture>().ToUniTask() ?? new UniTask<Texture>(null);

            _albedoInstance = await albedoTask;
            _normalInstance = await normalTask;
            _maskInstance = await maskTask;
        }

        protected override void ApplyModifications(Material material, KandraRenderer renderer) {
            material.SetTexture(AlbedoID, _albedoInstance);
            material.SetTexture(NormalID, _normalInstance);
            material.SetTexture(MaskID, _maskInstance);
            renderer.TexturesChanged();
        }

        protected override void CleanupModification(Material material, KandraRenderer renderer) {
            material.SetTexture(AlbedoID, null);
            material.SetTexture(NormalID, null);
            material.SetTexture(MaskID, null);
            renderer.TexturesChanged();
        }

        protected override void FinalizeCleanup() {
            _albedoReference?.ReleaseAsset();
            _normalTeReference?.ReleaseAsset();
            _maskReference?.ReleaseAsset();

            _albedoInstance = null;
            _normalInstance = null;
            _maskInstance = null;
        }
    }

    public abstract partial class SkinTexturesFeature<T> : SkinTexturesFeature where T : SkinTexturesFeature<T>, new() {
        public override BodyFeature GenericCopy() => Copy();

        public T Copy() {
            var copy = new T {
                _albedoReference = _albedoReference?.DeepCopy(),
                _normalTeReference = _normalTeReference?.DeepCopy(),
                _maskReference = _maskReference?.DeepCopy()
            };
            return copy;
        }
    }
}