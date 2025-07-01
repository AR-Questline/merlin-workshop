using UnityEngine;
using UnityEngine.Rendering.HighDefinition;

namespace Awaken.TG.Main.Settings.Controllers {
    [RequireComponent(typeof(HDAdditionalLightData))]
    public class LightSurfaceTextureQualityController : LightTexturesQualityController {
        protected override bool IsNullAssetValidOption => true;
        protected override bool IsAnyQualityAssetSet => _hdLight.surfaceTexture;

        protected override void SetAsset(Texture texture) {
            _hdLight.surfaceTexture = texture;
        }

    }
}