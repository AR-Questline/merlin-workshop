using Awaken.TG.Main.Settings.Graphics;
using Awaken.TG.MVC;
using UnityEngine;
using UnityEngine.Rendering.HighDefinition;

namespace Awaken.TG.Main.Settings.Controllers {
    /// <summary>
    /// Takes care of switching AntiAliasing options in camera, based on graphics settings.
    /// </summary>
    [RequireComponent(typeof(HDAdditionalCameraData))]
    public class AntiAliasingController : StartDependentView<AntiAliasing> {
        HDAdditionalCameraData _camera;
        
        protected override void OnInitialize() {
            _camera = GetComponent<HDAdditionalCameraData>();
            Target.ListenTo(Setting.Events.SettingRefresh, OnSettingChanged, this);
            OnSettingChanged(Target);
        }

        protected override void OnFullyInitialized() {
            base.OnFullyInitialized();
            World.Any<UpScaling>()?.ListenTo(Setting.Events.SettingRefresh, OnSettingChanged, this);
        }

        void OnSettingChanged(Setting _) {
            if (AntiAliasing.IsUpScalingWithAAEnabled) {
                _camera.antialiasing = HDAdditionalCameraData.AntialiasingMode.None;
                return;
            }
            AntiAliasing aa = Target;
            if (aa.TAA) {
                _camera.antialiasing = HDAdditionalCameraData.AntialiasingMode.TemporalAntialiasing;
            } else if (aa.SMAA) {
                _camera.antialiasing = HDAdditionalCameraData.AntialiasingMode.SubpixelMorphologicalAntiAliasing;
            } else if (aa.FXAA) {
                _camera.antialiasing = HDAdditionalCameraData.AntialiasingMode.FastApproximateAntialiasing;
            } else {
                _camera.antialiasing = HDAdditionalCameraData.AntialiasingMode.None;
            }
        }
    }
}