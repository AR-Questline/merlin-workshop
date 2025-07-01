using Awaken.TG.Main.Settings.Debug;
using Awaken.TG.MVC;
using Awaken.Utility.Graphics;
using Unity.Mathematics;
using UnityEngine.Rendering;
using UnityEngine.Rendering.HighDefinition;

namespace Awaken.TG.Main.Settings.Controllers {
    public class OldGpuFixer : StartDependentView<FlickerFixSetting>, IVolumeController {
        const float PhysicallyBasedSkySpaceEmissionMultiplierMaxValue = 150000;
        bool _enabled;
        Volume _volume;
        float _defaultSpaceEmissionMultiplierValue;

        protected override void OnInitialize() {
            var flickerFixSetting = World.Only<FlickerFixSetting>();
            flickerFixSetting.ListenTo(Setting.Events.SettingRefresh, OnSettingChanged, this);
            _volume = GetComponent<Volume>();
            if (_volume.TryGetVolumeComponent(out PhysicallyBasedSky physicallyBasedSky)) {
                _defaultSpaceEmissionMultiplierValue = physicallyBasedSky.spaceEmissionMultiplier.value;
            }

            _enabled = flickerFixSetting.Enabled;
            FixVolumes();
        }

        public void NewVolumeProfileLoaded() {
            FixVolumes();
        }

        void OnSettingChanged(Setting setting) {
            _enabled = ((FlickerFixSetting)setting).Enabled;
            FixVolumes();
        }

        void FixVolumes() {
            if (_volume.TryGetVolumeComponent(out PhysicallyBasedSky physicallyBasedSky)) {
                if (_enabled) {
                    physicallyBasedSky.spaceEmissionMultiplier.value = math.clamp(
                        physicallyBasedSky.spaceEmissionMultiplier.value,
                        0,
                        PhysicallyBasedSkySpaceEmissionMultiplierMaxValue);
                } else {
                    physicallyBasedSky.spaceEmissionMultiplier.value = _defaultSpaceEmissionMultiplierValue;
                }
            }

            if (_volume.TryGetVolumeComponent(out CloudLayer cloudLayer)) {
                cloudLayer.active = !_enabled;
            }
        }
    }
}