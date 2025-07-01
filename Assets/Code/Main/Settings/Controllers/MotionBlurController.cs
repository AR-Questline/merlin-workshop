using Awaken.TG.Graphics.VFX;
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
    public class MotionBlurController : StartDependentView<MotionBlurSetting>, IVolumeController {
        Volume _volume;
        VolumeProfile _profile;

        bool _usesBlur;
        MotionBlur _blur;

        bool _usesDirectionalBlur;
        float _directionalBlurIntensity;
        DirectionalBlur _directionalBlur;

        protected override void OnInitialize() {
            _volume = GetComponent<Volume>();
            Target.ListenTo(Setting.Events.SettingRefresh, OnSettingChanged, this);
            
            OnSettingChanged(Target);
        }
        
        public void NewVolumeProfileLoaded() {
            if (Target == null || _volume == null) {
                return;
            }
            _blur = null;
            _directionalBlur = null;
            OnSettingChanged(Target);
        }

        void TryInitializeVolumeComponents() {
            var newProfile = _volume.GetSharedOrInstancedProfile();

            if (newProfile == _profile) {
                return;
            }

            _usesBlur = false;
            _usesDirectionalBlur = false;
            _profile = newProfile;

            if (_volume.TryGetVolumeComponent(out _blur)) {
                _usesBlur = _blur.active;
            }
            
            if (_volume.TryGetVolumeComponent(out _directionalBlur)) {
                _usesDirectionalBlur = _directionalBlur.active;
                _directionalBlurIntensity = _directionalBlur.intensity.value;
            }
        }

        void OnSettingChanged(Setting setting) {
            TryInitializeVolumeComponents();

            MotionBlurSetting blur = (MotionBlurSetting)setting;

            if (_usesBlur) {
                _blur.intensity.value = blur.Enabled ? blur.Intensity : 0;
            }
            
            if (_usesDirectionalBlur) {
                _directionalBlur.intensity.value = blur.Enabled ? _directionalBlurIntensity : 0;
            }
        }
    }
}