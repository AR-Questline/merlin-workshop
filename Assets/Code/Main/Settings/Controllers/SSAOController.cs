using Awaken.TG.Main.Settings.Graphics;
using Awaken.TG.MVC;
using Awaken.Utility.Graphics;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.HighDefinition;

namespace Awaken.TG.Main.Settings.Controllers {
    /// <summary>
    /// Takes care of enabling/disabling SSAO in post process volumes, based on graphics settings.
    /// </summary>
    [RequireComponent(typeof(Volume))]
    public class SSAOController : StartDependentView<SSAO>, IVolumeController {
        Volume _volume;
        bool _usesSSAO;
        ScreenSpaceAmbientOcclusion _occlusion;
        
        protected override void OnInitialize() {
            _volume = GetComponent<Volume>();
            Target.ListenTo(Setting.Events.SettingRefresh, OnSettingChanged, this);
            OnSettingChanged(Target);
        }
        
        public void NewVolumeProfileLoaded() {
            if (Target == null || _volume == null) {
                return;
            }
            _occlusion = null;
            OnSettingChanged(Target);
        }

        void OnSettingChanged(Setting setting) {
            if (_occlusion == null) {
                if (!_volume.TryGetVolumeComponent(out _occlusion)) {
                    return;
                }
                _usesSSAO = _occlusion.active;
            }
            
            SSAO ssao = (SSAO) setting;
            _occlusion.active = ssao.Enabled && _usesSSAO;
        }
    }
}