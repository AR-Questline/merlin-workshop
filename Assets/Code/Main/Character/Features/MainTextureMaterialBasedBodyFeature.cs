using Awaken.Kandra;
using Awaken.TG.Assets;
using Awaken.TG.Utility.Attributes;
using Cysharp.Threading.Tasks;
using Newtonsoft.Json;
using UnityEngine;

namespace Awaken.TG.Main.Character.Features {
    public abstract partial class MainTextureMaterialBasedBodyFeature<T> : MaterialBasedBodyFeature where T : MainTextureMaterialBasedBodyFeature<T>, new() {
        [Saved] ARAssetReference _textureReference;

        Texture _instance;

        protected abstract int MainTextureShaderID { get; }

        [JsonConstructor, UnityEngine.Scripting.Preserve] protected MainTextureMaterialBasedBodyFeature() { }

        protected MainTextureMaterialBasedBodyFeature(ARAssetReference mainTexture) {
            _textureReference = mainTexture;
        }

        public sealed override BodyFeature GenericCopy() => Copy();

        public T Copy() {
            var copy = new T {
                _textureReference = _textureReference.DeepCopy(),
            };
            return copy;
        }

        protected sealed override async UniTask Initialize() {
            _instance = await _textureReference.LoadAsset<Texture>().ToUniTask();
        }

        protected sealed override void ApplyModifications(Material material, KandraRenderer renderer) {
            material.SetTexture(MainTextureShaderID, _instance);
            renderer.TexturesChanged();
        }

        protected sealed override void CleanupModification(Material material, KandraRenderer renderer) {
            material.SetTexture(MainTextureShaderID, null);
            renderer.TexturesChanged();
        }

        protected sealed override void FinalizeCleanup() {
            _textureReference.ReleaseAsset();
            _instance = null;
        }
    }
}
