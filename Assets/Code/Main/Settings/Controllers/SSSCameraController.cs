using Awaken.TG.Main.Settings.Graphics;
using Awaken.TG.MVC;
using UnityEngine;
using UnityEngine.Rendering.HighDefinition;
using static Awaken.TG.Main.Settings.Controllers.RenderingPathCustomFrameSettingsUtils;

namespace Awaken.TG.Main.Settings.Controllers {
    [RequireComponent(typeof(HDAdditionalCameraData))]
    public class SSSCameraController : StartDependentView<SSS> {
        HDAdditionalCameraData _cameraData;

        protected override void OnInitialize() {
            _cameraData = GetComponent<HDAdditionalCameraData>();
            Target.ListenTo(Setting.Events.SettingRefresh, OnSettingChanged, this);
            OnSettingChanged(Target);
        }

        void OnSettingChanged(Setting setting) {
            var sss = (SSS)setting;
            var overrideMask = _cameraData.renderingPathCustomFrameSettingsOverrideMask;
            ref var frameSettings = ref _cameraData.renderingPathCustomFrameSettings;

            Override(ref overrideMask, ref frameSettings, FrameSettingsField.SubsurfaceScattering, sss.Enabled);
            overrideMask.mask[(uint)FrameSettingsField.SssQualityLevel] = true;
            frameSettings.sssQualityLevel = sss.Quality;

            _cameraData.renderingPathCustomFrameSettingsOverrideMask = overrideMask;
        }
    }
}
