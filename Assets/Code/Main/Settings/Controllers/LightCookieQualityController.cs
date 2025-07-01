using UnityEngine;
using UnityEngine.Rendering.HighDefinition;

namespace Awaken.TG.Main.Settings.Controllers {
    [RequireComponent(typeof(HDAdditionalLightData))]
    public class LightCookieQualityController : LightTexturesQualityController {
        protected override bool IsNullAssetValidOption => true;
        protected override bool IsAnyQualityAssetSet => _hdLight.areaLightCookie != null || _light.cookie != null;

        protected override void SetAsset(Texture texture) {
            if (texture == null) {
                _hdLight.areaLightCookie = null;
                _light.cookie = null;
                return;
            }
            _hdLight.SetCookie(texture, new Vector2(_hdLight.shapeWidth, _hdLight.shapeHeight));
        }
    }
}