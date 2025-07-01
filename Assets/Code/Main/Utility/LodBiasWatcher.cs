using Awaken.TG.Main.Settings.Controllers;
using Awaken.TG.Main.Settings.Graphics;
using UnityEngine;
using UnityEngine.Rendering.HighDefinition;

namespace Awaken.TG.Main.Utility {
    [RequireComponent(typeof(HDAdditionalCameraData))]
    public class LodBiasWatcher : StartDependentView<DistanceCullingSetting> {
        HDAdditionalCameraData _cameraData;
        
        protected override void OnInitialize() {
            _cameraData = GetComponent<HDAdditionalCameraData>();
        }
        
        protected void Update() {
            if (ReferenceEquals(_cameraData, null)) {
                return;
            }

            var frameSettings = _cameraData.renderingPathCustomFrameSettings;
            var pipelineAsset = QualitySettings.GetRenderPipelineAssetAt(QualitySettings.GetQualityLevel()) as HDRenderPipelineAsset;
            if (QualitySettings.lodBias != frameSettings.GetResolvedLODBias(pipelineAsset)) {
                Target.RefreshSetting();
            }
        }
    }
}