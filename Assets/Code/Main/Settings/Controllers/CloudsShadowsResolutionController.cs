using Awaken.TG.Main.Settings.Graphics;
using Awaken.Utility.Graphics;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.HighDefinition;

namespace Awaken.TG.Main.Settings.Controllers {
    [RequireComponent(typeof(Volume))]
    public class CloudsShadowsResolutionController : StartDependentView<GeneralGraphics>, IVolumeController, IGeneralGraphicsSceneView {
        Volume _volume;
        CloudLayer _clouds;

        protected override void OnInitialize() {
            _volume = GetComponent<Volume>();
            OnSettingChanged(Target);
        }

        public void NewVolumeProfileLoaded() {
            if (Target == null || _volume == null) {
                return;
            }
            _clouds = null;
            OnSettingChanged(Target);
        }

        public void SettingsRefreshed(GeneralGraphics graphicsSetting) {
            OnSettingChanged(graphicsSetting);
        }

        void OnSettingChanged(GeneralGraphics graphicsSetting) {
            if (_clouds == null) {
                _volume.TryGetVolumeComponent(out _clouds);
            }

            if (_clouds == null) {
                return;
            }
            _clouds.shadowResolution.Override(GetCloudShadowResolutionByQuality(graphicsSetting.ActiveIndex));
        }

        CloudShadowsResolution GetCloudShadowResolutionByQuality(int qualityIndex) {
            return qualityIndex switch {
                0 => CloudShadowsResolution.Low,
                1 => CloudShadowsResolution.Medium,
                2 => CloudShadowsResolution.High,
                _ => CloudShadowsResolution.Low
            };
        }
    }
}