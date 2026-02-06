using Awaken.TG.Main.Settings.Graphics;
using Awaken.TG.MVC;
using Awaken.Utility.Graphics;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.HighDefinition;

namespace Awaken.TG.Main.Settings.Controllers {
    /// <summary>
    /// Takes care of enabling/disabling Vignette in post process volumes, based on graphics settings.
    /// </summary>
    [RequireComponent(typeof(Volume))]
    public class VignetteController : StartDependentView<VignetteSetting>, IVolumeController {
        Volume _volume;
        bool _usesVignette;
        Vignette _vignette;

        protected override void OnInitialize() {
            _volume = GetComponent<Volume>();
            Target.ListenTo(Setting.Events.SettingRefresh, OnSettingChanged, this);

            if (!_volume.TryGetVolumeComponent(out _vignette)) {
                return;
            }

            _usesVignette = _vignette.active;
            OnSettingChanged(Target);
        }

        public void NewVolumeProfileLoaded() {
            if (Target == null || _volume == null) {
                return;
            }

            _vignette = null;
            OnSettingChanged(Target);
        }

        void OnSettingChanged(Setting setting) {
            if (_vignette == null) {
                if (!_volume.TryGetVolumeComponent(out _vignette)) {
                    return;
                }

                _usesVignette = _vignette.active;
            }

            VignetteSetting vignette = (VignetteSetting)setting;
            _vignette.active = vignette.Enabled && _usesVignette;
        }
    }
}