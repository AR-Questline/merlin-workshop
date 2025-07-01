using Awaken.TG.Main.Settings.Graphics;
using Awaken.TG.MVC;
using UnityEngine;
using UnityEngine.Rendering.HighDefinition;
using static Awaken.TG.Main.Settings.Controllers.RenderingPathCustomFrameSettingsUtils;

namespace Awaken.TG.Main.Settings.Controllers {
    [RequireComponent(typeof(HDAdditionalCameraData))]
    public class ReflectionsCameraController : StartDependentView<Reflections> {
        HDAdditionalCameraData _cameraData;

        protected override void OnInitialize() {
            _cameraData = GetComponent<HDAdditionalCameraData>();
            Target.ListenTo(Setting.Events.SettingRefresh, OnSettingChanged, this);
            OnSettingChanged(Target);
        }

        void OnSettingChanged(Setting setting) {
            var reflections = (Reflections)setting;
            var overrideMask = _cameraData.renderingPathCustomFrameSettingsOverrideMask;
            ref var frameSettings = ref _cameraData.renderingPathCustomFrameSettings;

            Override(ref overrideMask, ref frameSettings, FrameSettingsField.SSR, reflections.Enabled);
            Override(ref overrideMask, ref frameSettings, FrameSettingsField.TransparentSSR, reflections.Enabled);
            Override(ref overrideMask, ref frameSettings, FrameSettingsField.ReflectionProbe, reflections.Enabled);
            Override(ref overrideMask, ref frameSettings, FrameSettingsField.PlanarProbe, reflections.Enabled);

            _cameraData.renderingPathCustomFrameSettingsOverrideMask = overrideMask;
        }
    }
}
