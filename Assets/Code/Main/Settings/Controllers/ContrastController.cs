using Awaken.TG.Main.Settings.Gameplay;
using Awaken.TG.Main.UI.PhotoMode;
using Awaken.TG.MVC;
using Awaken.TG.MVC.Events;
using Awaken.Utility.Graphics;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.HighDefinition;

namespace Awaken.TG.Main.Settings.Controllers {
    [RequireComponent(typeof(Volume))]
    public class ContrastController : StartDependentView<ContrastSetting> {
        Volume _volume;
        ColorAdjustments _colorAdjustments;

        protected override void OnInitialize() {
            _volume = GetComponent<Volume>();
            if (!_volume.isGlobal) return;
            Target.ListenTo(Setting.Events.SettingRefresh, OnSettingChanged, this);
            World.EventSystem.ListenTo(EventSelector.AnySource, PhotoModeUI.Events.ContrastChanged, this, SetContrastValue);
            OnSettingChanged(Target);
        }

        void OnSettingChanged(Setting setting) {
            ContrastSetting contrastSetting = (ContrastSetting) setting;
            SetContrastValue(contrastSetting.Value);
        }
        
        void SetContrastValue(float value) {
            if (_colorAdjustments == null) {
                _volume.TryGetVolumeComponent(out _colorAdjustments);
            }

            if (_colorAdjustments != null) {
                _colorAdjustments.contrast.value = math.remap(0f, 1f, -20f, 20f, value);
            }
        }
    }
}