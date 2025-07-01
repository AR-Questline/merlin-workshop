using Awaken.TG.Assets;
using UnityEngine;
using UnityEngine.Rendering.HighDefinition;

namespace Awaken.TG.Main.Settings.Controllers {
    [RequireComponent(typeof(HDAdditionalLightData))]
    public abstract class LightTexturesQualityController : AssetQualityController<Texture> {
        [SerializeField] bool dontSetIfNotInitiallySet = true;
        [SerializeField, ARAssetReferenceSettings(new[] { typeof(Texture) }, true)] ARAssetReference lowQualityTexture;
        [SerializeField, ARAssetReferenceSettings(new[] { typeof(Texture) }, true)] ARAssetReference highQualityTexture;
        [SerializeField, ARAssetReferenceSettings(new[] { typeof(Texture) }, true)] ARAssetReference fallbackTexture;
        protected HDAdditionalLightData _hdLight;
        protected Light _light;
        protected override bool DontSetIfNotInitiallySet => dontSetIfNotInitiallySet;
        protected override int QualityLevelsCount => 2;
        
        protected override void OnInitialize() {
            base.OnInitialize();
            EnsureValidQualityOptionsTarget();
        }

        protected override bool EnsureValidQualityOptionsTarget() {
            if (_hdLight != null && _light != null) {
                return true;
            }
            _hdLight = GetComponent<HDAdditionalLightData>();
            _light = GetComponent<Light>();
            return _hdLight != null;
        }

        protected override ARAssetReference GetAssetForQuality(int qualityIndex) {
            return qualityIndex switch {
                0 => lowQualityTexture,
                _ => highQualityTexture
            };
        }
        protected override ARAssetReference GetFallbackAsset() => fallbackTexture;
    }
}