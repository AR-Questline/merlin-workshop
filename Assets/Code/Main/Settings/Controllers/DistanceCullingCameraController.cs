using Awaken.TG.Main.Settings.Graphics;
using Awaken.TG.Main.Utility;
using Awaken.TG.MVC;
using Awaken.Utility.GameObjects;
using UnityEngine;
using UnityEngine.Rendering.HighDefinition;
using static Awaken.TG.Main.Settings.Controllers.RenderingPathCustomFrameSettingsUtils;

namespace Awaken.TG.Main.Settings.Controllers {
    [RequireComponent(typeof(HDAdditionalCameraData))]
    public class DistanceCullingCameraController : StartDependentView<DistanceCullingSetting> {
        HDAdditionalCameraData _cameraData;
        
        protected override void OnInitialize() {
            _cameraData = GetComponent<HDAdditionalCameraData>();
            Target.ListenTo(Setting.Events.SettingRefresh, OnSettingChanged, this);
            OnSettingChanged(Target);
            
            if (gameObject.HasComponent<LodBiasWatcher>() == false) {
                gameObject.AddComponent<LodBiasWatcher>();
            }
        }

        void OnSettingChanged(Setting setting) {
            var cullingSetting = (DistanceCullingSetting)setting;
            var overrideMask = _cameraData.renderingPathCustomFrameSettingsOverrideMask;
            ref var frameSettings = ref _cameraData.renderingPathCustomFrameSettings;

            frameSettings.lodBiasMode = LODBiasMode.OverrideQualitySettings;
            Override(ref overrideMask, ref frameSettings, FrameSettingsField.LODBiasMode, true);
            frameSettings.lodBias = cullingSetting.BiasValue;
            Override(ref overrideMask, ref frameSettings, FrameSettingsField.LODBias, true);

            _cameraData.renderingPathCustomFrameSettingsOverrideMask = overrideMask;
        }
    }
}
