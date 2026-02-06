using Awaken.TG.Main.Settings.Graphics;
using Awaken.TG.MVC;
using Awaken.Utility.Debugging;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.HighDefinition;

namespace Awaken.TG.Main.Settings.Controllers {
    [RequireComponent(typeof(HDAdditionalCameraData))]
    public class UpScalingController : StartDependentView<UpScaling> {
        const float DefaultScaling = 100;

        HDAdditionalCameraData _camera;
        
        protected override void OnInitialize() {
            _camera = GetComponent<HDAdditionalCameraData>();
            _camera.deepLearningSuperSamplingUseOptimalSettings = false;
            Target.ListenTo(Setting.Events.SettingRefresh, OnSettingChanged, this);
            
            OnSettingChanged(Target);
        }
        
        // === Public API
        public void EnableUpScaling() {
            var upScalingType = Target.ActiveUpScalingType;

            switch (upScalingType) {
                case UpScalingType.None:
                    DisableUpScaling();
                    break;
#if !UNITY_GAMECORE && !UNITY_PS5
                case UpScalingType.DLSS:
                    EnableDLSS();
                    break;
#endif
                case UpScalingType.STP:
                    EnableSTP();
                    break;
                default:
                    Log.Important?.Error($"UpScaling enabling is not implemented for type {Target.ActiveUpScalingType}. Disabling UpScaling");
                    DisableUpScaling();
                    break;
            }
        }

        public void DisableUpScaling() {
            DynamicResolutionHandler.SetDynamicResScaler(() => DefaultScaling, DynamicResScalePolicyType.ReturnsPercentage);
            DynamicResolutionHandler.SetActiveDynamicScalerSlot(DynamicResScalerSlot.User);
           
            _camera.allowDeepLearningSuperSampling = false;
            _camera.deepLearningSuperSamplingUseCustomQualitySettings = false;
            _camera.allowDynamicResolution = false;
        }

        // === Private methods
        void OnSettingChanged(Setting setting) {
            if (UpScaling.IsAnyUpScalingAvailable == false) {
                return;
            }

            EnableUpScaling();
        }
        
        void EnableSTP() {
            if (UpScaling.IsSTPAvailable == false) {
                DisableUpScaling();
                return;
            }
            
            _camera.allowDynamicResolution = true;
            _camera.allowDeepLearningSuperSampling = false;
            _camera.deepLearningSuperSamplingUseCustomQualitySettings = false;
            
            float stpScaling = Target.IsSTPEnabled ? Target.QualityScaling : 100;
            DynamicResolutionHandler.SetDynamicResScaler(() => stpScaling, DynamicResScalePolicyType.ReturnsPercentage);
            DynamicResolutionHandler.SetActiveDynamicScalerSlot(DynamicResScalerSlot.User);
        }
        
#if !UNITY_GAMECORE && !UNITY_PS5
        void EnableDLSS() {
            if (UpScaling.IsDLSSAvailable == false) {
                DisableUpScaling();
                return;
            }

            _camera.deepLearningSuperSamplingQuality = (uint)Target.DLSSQuality;
            _camera.allowDynamicResolution = true;
            _camera.allowDeepLearningSuperSampling = true;
            _camera.deepLearningSuperSamplingUseCustomQualitySettings = true;
            
            float dlssScaling = Target.IsDLSSEnabled ? Target.QualityScaling : 100;
            DynamicResolutionHandler.SetDynamicResScaler(() => dlssScaling, DynamicResScalePolicyType.ReturnsPercentage);
            DynamicResolutionHandler.SetActiveDynamicScalerSlot(DynamicResScalerSlot.User);
        }

#endif
    }
}