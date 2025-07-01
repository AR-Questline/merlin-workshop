using Awaken.Utility.Maths;
using UnityEngine;
using UnityEngine.Rendering.HighDefinition;

namespace Awaken.Utility.Graphics {
    public static class LightUtils {
        public static Bounds CalculateApproximateLightBounds(HDAdditionalLightData lightData) {
            var light = lightData.GetComponent<Light>();
            Bounds bounds = default;
            if (light.type is LightType.Point or LightType.Rectangle) {
                bounds = new(lightData.transform.position, (lightData.range * 2).UniformVector3());
            }
            if (light.type == LightType.Spot) {
                // For pyramid is just approximation
                var center = new Vector3(0, 0, lightData.range / 2);
                var angle = lightData.GetComponent<Light>().spotAngle;
                var extent = Mathf.Sin(angle / 2 * Mathf.Deg2Rad) * lightData.range;
                var size = new Vector3(extent * 2, extent * 2, lightData.range);
                var localBounds = new Bounds(center, size);
                bounds = localBounds.ToWorld(lightData.transform);
            }
            return bounds;
        }
    }
}
