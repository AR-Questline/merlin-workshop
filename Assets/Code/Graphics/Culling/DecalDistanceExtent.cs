using Awaken.Utility.Debugging;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.Rendering.HighDefinition;

namespace Awaken.TG.Graphics.Culling {
    [RequireComponent(typeof(DecalProjector))]
    public class DecalDistanceExtent : MonoBehaviour {
        [PropertyRange(5, nameof(MaxDrawDistance)), OnValueChanged(nameof(DistanceChanged))]
        public float distance = StaticDecalsCuller.DefaultDrawDistance;

        void DistanceChanged() {
            var decalProjector = GetComponent<DecalProjector>();
            decalProjector.drawDistance = distance;
        }

        float MaxDrawDistance() {
            var currentHDRenderPipelineAsset = QualitySettings.GetRenderPipelineAssetAt(QualitySettings.GetQualityLevel()) as HDRenderPipelineAsset;
            if (currentHDRenderPipelineAsset == null) {
                Log.Critical?.Error("There is no HDRP asset in the current quality settings.");
                return 200;
            }

            var renderPipelineSettings = currentHDRenderPipelineAsset.currentPlatformRenderPipelineSettings;
            var decalSettings = renderPipelineSettings.decalSettings;
            var hdrpSettingsDecalsDrawDistance = decalSettings.drawDistance;
            return hdrpSettingsDecalsDrawDistance;
        }
    }
}