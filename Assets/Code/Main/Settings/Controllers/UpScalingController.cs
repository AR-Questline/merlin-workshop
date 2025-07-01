using Awaken.TG.Main.Heroes.CharacterCreators;
using Awaken.TG.Main.Heroes.CharacterSheet;
using Awaken.TG.Main.Settings.GammaSettingScreen;
using Awaken.TG.Main.Settings.Graphics;
using Awaken.TG.MVC;
using Awaken.TG.MVC.Events;
using Awaken.Utility.Animations;
using Awaken.Utility.Debugging;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.HighDefinition;

namespace Awaken.TG.Main.Settings.Controllers {
    /// <summary>
    /// Takes care of switching AntiAliasing options in camera, based on graphics settings.
    /// </summary>
    [RequireComponent(typeof(HDAdditionalCameraData))]
    public class UpScalingController : StartDependentView<UpScaling> {
        const float DefaultScaling = 100;

        HDAdditionalCameraData _camera;
        float _dlssScaling;
        
        protected override void OnInitialize() {
            _camera = GetComponent<HDAdditionalCameraData>();
            _camera.deepLearningSuperSamplingUseOptimalSettings = false;
            _dlssScaling = DefaultScaling;
            Target.ListenTo(Setting.Events.SettingRefresh, OnSettingChanged, this);
            
            OnSettingChanged(Target);
        }
        
        // === Public API
        public void EnableUpScaling() {
            DisableUpScaling(true);
            var upScalingType = Target.ActiveUpScalingType;

            switch (upScalingType) {
                case UpScalingType.None:
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
                    Log.Important?.Error(
                        $"UpScaling enabling is not implemented for type {Target.ActiveUpScalingType}. Disabling UpScaling");
                    break;
            }
        }

        public void DisableUpScaling(bool temporaryDisable = false) {
            DynamicResolutionHandler.SetDynamicResScaler(() => DefaultScaling, DynamicResScalePolicyType.ReturnsPercentage);
            DynamicResolutionHandler.SetActiveDynamicScalerSlot(DynamicResScalerSlot.User);
            if ((temporaryDisable && Target.ActiveUpScalingType == UpScalingType.STP) == false) {
                DisableForceUpScalingResolution();
            }
           
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
                return;
            }
            _camera.allowDynamicResolution = true;
            float stpScaling = Target.IsSTPEnabled ? Target.QualityScaling : 100;
            EnableForceFixedMaxUpScalingResolution(stpScaling);
        }
        
#if !UNITY_GAMECORE && !UNITY_PS5
        void EnableDLSS() {
            if (UpScaling.IsDLSSAvailable == false) {
                return;
            }

            _camera.deepLearningSuperSamplingQuality = (uint)Target.DLSSQuality;
            _dlssScaling = Target.IsDLSSEnabled ? Target.QualityScaling : 100;
            _camera.allowDynamicResolution = true;
            _camera.allowDeepLearningSuperSampling = true;
            _camera.deepLearningSuperSamplingUseCustomQualitySettings = true;
            DynamicResolutionHandler.SetDynamicResScaler(() => _dlssScaling,
                DynamicResScalePolicyType.ReturnsPercentage);
            DynamicResolutionHandler.SetActiveDynamicScalerSlot(DynamicResScalerSlot.User);
        }

#endif
        static void DisableForceUpScalingResolution() {
            var currentHDRenderPipelineAsset = QualitySettings.GetRenderPipelineAssetAt(QualitySettings.GetQualityLevel()) as HDRenderPipelineAsset;
            if (currentHDRenderPipelineAsset != null) {
                var settings = currentHDRenderPipelineAsset.currentPlatformRenderPipelineSettings;
                if (settings.dynamicResolutionSettings.forceResolution) {
                    settings.dynamicResolutionSettings.forceResolution = false;
                    currentHDRenderPipelineAsset.currentPlatformRenderPipelineSettings = settings;
                }
            }
        }

        static void EnableForceFixedMaxUpScalingResolution(float forcedPercentage) {
            var currentHDRenderPipelineAsset = QualitySettings.GetRenderPipelineAssetAt(QualitySettings.GetQualityLevel()) as HDRenderPipelineAsset;
            if (currentHDRenderPipelineAsset == null) {
                return;
            }
            var settings = currentHDRenderPipelineAsset.currentPlatformRenderPipelineSettings;
            if (settings.dynamicResolutionSettings.forceResolution == false || settings.dynamicResolutionSettings.forcedPercentage != forcedPercentage) {
                settings.dynamicResolutionSettings.forceResolution = true;
                settings.dynamicResolutionSettings.forcedPercentage = forcedPercentage;
                currentHDRenderPipelineAsset.currentPlatformRenderPipelineSettings = settings;
            }
        }
    }
}