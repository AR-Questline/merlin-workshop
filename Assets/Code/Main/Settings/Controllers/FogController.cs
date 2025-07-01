using Awaken.TG.Main.Settings.Graphics;
using Awaken.TG.MVC;
using Awaken.Utility.Graphics;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.HighDefinition;

namespace Awaken.TG.Main.Settings.Controllers {
    /// <summary>
    /// Takes care of enabling/disabling Fog in post process volumes, based on graphics settings.
    /// </summary>
    [RequireComponent(typeof(Volume))]
    public class FogController : StartDependentView<FogQuality>, IVolumeController {
        Volume _volume;
        bool _usesFog;
        int _maxLevel;
        bool _usesCustom;
        Fog _fog;
        
        protected override void OnInitialize() {
            _volume = GetComponent<Volume>();
            Target.ListenTo(Setting.Events.SettingRefresh, OnSettingChanged, this);
            
            if (!_volume.TryGetVolumeComponent(out _fog)) {
                return;
            }
            
            _usesFog = _fog.active;
            (_maxLevel, _usesCustom) = _fog.quality.levelAndOverride;
            if (_usesCustom) {
                _maxLevel = 2;
            }
            OnSettingChanged(Target);
        }
        
        public void NewVolumeProfileLoaded() {
            if (Target == null || _volume == null) {
                return;
            }
            _fog = null;
            OnSettingChanged(Target);
        }

        void OnSettingChanged(Setting setting) {
            if (_fog == null) {
                if (!_volume.TryGetVolumeComponent(out _fog)) {
                    return;
                }
                _usesFog = _fog.active;
            }
            
            FogQuality fogQuality = (FogQuality) setting;
            if (_usesFog) {
                if (fogQuality.Quality == fogQuality.MaxQuality) {
                    _fog.quality.levelAndOverride = (_maxLevel, _usesCustom);
                } else {
                    int diff = fogQuality.Quality - fogQuality.MaxQuality;
                    int q = Mathf.Clamp(_maxLevel + diff, 0, 2);
                    _fog.quality.levelAndOverride = (q, false);
                }
            }
        }
    }
}