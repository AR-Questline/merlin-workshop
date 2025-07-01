using Awaken.TG.Main.Settings.Graphics;
using Awaken.TG.MVC;
using Awaken.Utility.Graphics;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.HighDefinition;

namespace Awaken.TG.Main.Settings.Controllers {
    /// <summary>
    /// Takes care of enabling/disabling Motion Blur in post process volumes, based on graphics settings.
    /// </summary>
    [RequireComponent(typeof(Volume))]
    public class ChromaticAberrationController : StartDependentView<ChromaticAberrationSetting>, IVolumeController {
        Volume _volume;
        bool _usesChromaticAberration;
        ChromaticAberration _chromaticAberration;

        protected override void OnInitialize() {
            _volume = GetComponent<Volume>();
            Target.ListenTo(Setting.Events.SettingRefresh, OnSettingChanged, this);

            if (!_volume.TryGetVolumeComponent(out _chromaticAberration)) {
                return;
            }

            _usesChromaticAberration = _chromaticAberration.active;
            OnSettingChanged(Target);
        }

        public void NewVolumeProfileLoaded() {
            if (Target == null || _volume == null) {
                return;
            }

            _chromaticAberration = null;
            OnSettingChanged(Target);
        }

        void OnSettingChanged(Setting setting) {
            if (_chromaticAberration == null) {
                if (!_volume.TryGetVolumeComponent(out _chromaticAberration)) {
                    return;
                }

                _usesChromaticAberration = _chromaticAberration.active;
            }

            ChromaticAberrationSetting chromaticAberration = (ChromaticAberrationSetting)setting;
            _chromaticAberration.active = chromaticAberration.Enabled && _usesChromaticAberration;
        }
    }
}