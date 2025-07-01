using Awaken.Kandra;
using Awaken.TG.Utility.Attributes;
using Cysharp.Threading.Tasks;
using Newtonsoft.Json;
using UnityEngine;

namespace Awaken.TG.Main.Character.Features {
    public abstract partial class TattooFeature : MaterialBasedBodyFeature {
        protected static readonly int WyrdlookToggle = Shader.PropertyToID("_TattooWyrdlook");

        protected static readonly int TattooMap = Shader.PropertyToID("_TattooMaskMap");
        protected static readonly int TattooNormalMap = Shader.PropertyToID("_TattooNormalMap");

        protected static readonly int TattooColorA = Shader.PropertyToID("_TattooColorA");
        protected static readonly int TattooColorB = Shader.PropertyToID("_TattooColorB");
        protected static readonly int TattooColorC = Shader.PropertyToID("_TattooColorC");
        protected static readonly int TattooColorD = Shader.PropertyToID("_TattooColorD");

        protected static readonly int TattooEmissiveA = Shader.PropertyToID("_TattooEmissiveA");
        protected static readonly int TattooEmissiveB = Shader.PropertyToID("_TattooEmissiveB");
        protected static readonly int TattooEmissiveC = Shader.PropertyToID("_TattooEmissiveC");
        protected static readonly int TattooEmissiveD = Shader.PropertyToID("_TattooEmissiveD");

        [Saved] protected TattooConfig _config;

        Texture _mapInstance;
        Texture _normalInstance;
        bool _applied;

        [JsonConstructor, UnityEngine.Scripting.Preserve] protected TattooFeature() { }

        protected TattooFeature(in TattooConfig config) {
            _config = config;
        }

        protected sealed override async UniTask Initialize() {
            var mapTask = LoadMapTexture();
            var normalMapTask = LoadNormalMapTexture();

            _mapInstance = await mapTask;
            _normalInstance = await normalMapTask;
        }

        protected abstract UniTask<Texture> LoadMapTexture();
        protected abstract UniTask<Texture> LoadNormalMapTexture();

        protected override void ApplyModifications(Material material, KandraRenderer renderer) {
            if (!material.HasTexture(TattooMap) || material.GetTexture(TattooMap) != null) {
                return;
            }

            material.SetInt(WyrdlookToggle, 1);
            material.SetTexture(TattooMap, _mapInstance);
            material.SetTexture(TattooNormalMap, _normalInstance);

            material.SetColor(TattooColorA, _config.colorA);
            material.SetColor(TattooColorB, _config.colorB);
            material.SetColor(TattooColorC, _config.colorC);
            material.SetColor(TattooColorD, _config.colorD);

            material.SetFloat(TattooEmissiveA, _config.emissiveA);
            material.SetFloat(TattooEmissiveB, _config.emissiveB);
            material.SetFloat(TattooEmissiveC, _config.emissiveC);
            material.SetFloat(TattooEmissiveD, _config.emissiveD);
            renderer.TexturesChanged();

            _applied = true;
        }

        protected override void CleanupModification(Material material, KandraRenderer renderer) {
            if (!_applied) {
                return;
            }
            _applied = false;
            material.SetInt(WyrdlookToggle, 0);
            material.SetTexture(TattooMap, null);
            material.SetTexture(TattooNormalMap, null);
            material.SetColor(TattooColorA, Color.white);
            material.SetColor(TattooColorB, Color.white);
            material.SetColor(TattooColorC, Color.white);
            material.SetColor(TattooColorD, Color.white);
            material.SetFloat(TattooEmissiveA, 0);
            material.SetFloat(TattooEmissiveB, 0);
            material.SetFloat(TattooEmissiveC, 0);
            material.SetFloat(TattooEmissiveD, 0);
            renderer.TexturesChanged();
        }

        protected override void FinalizeCleanup() {
            _config.Face?.ReleaseAsset();
            _config.Torso?.ReleaseAsset();
            _config.FaceNormal?.ReleaseAsset();
            _config.TorsoNormal?.ReleaseAsset();
            _mapInstance = null;
            _normalInstance = null;
        }
    }

    public abstract partial class TattooFeature<T> : TattooFeature where T : TattooFeature<T>, new() {
        [JsonConstructor, UnityEngine.Scripting.Preserve] protected TattooFeature() : base() { }

        protected TattooFeature(in TattooConfig config) : base(config) {
            _config = config;
        }

        public override BodyFeature GenericCopy() => Copy();

        public T Copy() {
            var copy = new T {
                _config = _config.Copy()
            };
            return copy;
        }
    }
}