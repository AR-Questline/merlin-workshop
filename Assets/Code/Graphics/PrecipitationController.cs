using System;
using System.Collections.Generic;
using System.Linq;
using Awaken.TG.Graphics.VFX;
using Awaken.TG.Main.AudioSystem.Biomes;
using Awaken.TG.Main.Settings.Controllers;
using Awaken.Utility.Maths;
using Awaken.Utility.Maths.Data;
using AwesomeTechnologies.VegetationSystem;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.HighDefinition;
using UnityEngine.Serialization;
using UnityEngine.VFX;

namespace Awaken.TG.Graphics {
    public class PrecipitationController : StartDependentView<WeatherController> {
        const float VisiblePrecipitationIntensity = 0.05f;
        static readonly int RainIntensityId = Shader.PropertyToID("RainIntensity");
        static readonly int SnowIntensityId = Shader.PropertyToID("SnowIntensity");

        [Header("References")]
        [SerializeField, Required] VisualEffect vfx;
        [SerializeField, Required] Volume rainVolume;
        [SerializeField] LocalVolumetricFog localVolumetricFog;
        [SerializeField, Required] CustomPassVolume screenSpaceWetnessVolume;
        [SerializeField, Required] TopDownDepthTexturesLoadingManager topDownDepthTexturesLoadingManager;
        [FormerlySerializedAs("_vspBiomeManager"), SerializeField] VSPBiomeManager vspBiomeManager;
        [SerializeField] List<Material> waterSurfaceCustomMaterials = new();

        [Header("Settings")]
        [SerializeField, Range(1, 64)] float localVolumetricFogDensity = 4f;
        [SerializeField] float _rainBlendSpeed = 0.5f;
        [SerializeField] float _snowBlendSpeed = 0.5f;
        [SerializeField] BiomeType[] _snowBiomeTypes = Array.Empty<BiomeType>();
        [SerializeField, Range(0, 1)] float dryingRate = .05f;

        [Space]
        [ShowInInspector, Range(0, 1)] float _moisture;

#if UNITY_EDITOR
        [ShowInInspector] bool _manualControl;
        [ShowInInspector, Range(0, 1)] float _editorIntensity;
        [ShowInInspector, Range(0, 1)] float _editorRainIntensity;
        [ShowInInspector, Range(0, 1)] float _editorSnowIntensity;
#endif
        public static bool ForceRain;
        float Intensity => Target.PrecipitationIntensity;
        float RainIntensity => Target.RainIntensity;
        float SnowIntensity => Target.SnowIntensity;

#if UNITY_EDITOR
        [ShowInInspector] float IntensityPreview => _manualControl || !Target ? _editorIntensity : Intensity;
        [ShowInInspector] float RainIntensityPreview => _manualControl || !Target ? _editorRainIntensity : RainIntensity;
        [ShowInInspector] float SnowIntensityPreview => _manualControl || !Target ? _editorSnowIntensity : SnowIntensity;
#endif

        ScreenSpaceWetness _screenSpaceWetness;

        void Start() {
            if (!Application.isPlaying) {
                return;
            }
            
            _screenSpaceWetness = screenSpaceWetnessVolume == null ?
                null :
                screenSpaceWetnessVolume.customPasses.OfType<ScreenSpaceWetness>().FirstOrDefault();
            if (_screenSpaceWetness != null && topDownDepthTexturesLoadingManager != null) {
                _screenSpaceWetness.SetDepthReferences(topDownDepthTexturesLoadingManager);
            }
            
            var waterSurfaces = GameObject.FindObjectsByType<WaterSurface>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            foreach (var ws in waterSurfaces) {
                if (ws.customMaterial != null && ws.customMaterial.HasProperty("_RainIntensity")) {
                    waterSurfaceCustomMaterials.Add(ws.customMaterial);
                }
            }
        }

        void Update() {
            if (!LightController.EditorPreviewUpdates) {
                return;
            }

#if UNITY_EDITOR
            if (_manualControl) {
                rainVolume.weight = _editorIntensity;
                vfx.SetFloat(RainIntensityId, _editorRainIntensity);
                vfx.SetFloat(SnowIntensityId, _editorSnowIntensity);
                
                foreach (var mat in waterSurfaceCustomMaterials) {
                    mat.SetFloat("_RainIntensity", _editorRainIntensity);
                }
                
                LocalVolumetricFogControl(_editorIntensity);
                UpdateScreenSpaceWetness(_editorIntensity, _editorRainIntensity);

                if (topDownDepthTexturesLoadingManager != null) {
                    topDownDepthTexturesLoadingManager.SetDepthTexturesLoadingEnabled(_editorIntensity > VisiblePrecipitationIntensity);
                }
                return;
            }
            if (!Application.isPlaying) {
                return;
            }
#endif

            if (!Target) {
                return;
            }
            UpdateBlendIns();
            var intensity = ForceRain ? 0.8f : Intensity;
            var rainIntensity = ForceRain ? 1 : RainIntensity; 
            rainVolume.weight = intensity;
            vfx.SetFloat(RainIntensityId, rainIntensity);
            vfx.SetFloat(SnowIntensityId, SnowIntensity);
            
            foreach (var mat in waterSurfaceCustomMaterials) {
                mat.SetFloat("_RainIntensity", rainIntensity);
            }
            
            LocalVolumetricFogControl(rainIntensity);
            UpdateScreenSpaceWetness(intensity, rainIntensity);

            if (topDownDepthTexturesLoadingManager != null) {
                topDownDepthTexturesLoadingManager.SetDepthTexturesLoadingEnabled(intensity > VisiblePrecipitationIntensity);
            }
        }

        void UpdateBlendIns() {
            ref var snowBlendIn = ref Target.SnowBlendIn;
            ref var rainBlendIn = ref Target.RainBlendIn;
            if (vspBiomeManager && Array.IndexOf(_snowBiomeTypes, vspBiomeManager.BiomeType) != -1) {
                snowBlendIn.Set(1f);
                snowBlendIn.Update(Time.deltaTime, _snowBlendSpeed);
                if (BlendProgress(snowBlendIn) >= 0.9f) {
                    rainBlendIn.Set(0);
                    rainBlendIn.Update(Time.deltaTime, _rainBlendSpeed);
                }
            } else {
                snowBlendIn.Set(0f);
                rainBlendIn.Set(1f);
                rainBlendIn.Update(Time.deltaTime, _rainBlendSpeed);
                if (BlendProgress(rainBlendIn) >= 0.9f) {
                    snowBlendIn.Update(Time.deltaTime, _snowBlendSpeed);
                }
            }
        }

        void LocalVolumetricFogControl(float intensity) {
            if (localVolumetricFog != null) {
                localVolumetricFog.parameters.meanFreePath = Mathf.Lerp(400f, localVolumetricFogDensity, intensity + 0.001f);
            }
        }

        void UpdateScreenSpaceWetness(float precipitationIntensity, float rainIntensity) {
            if (_screenSpaceWetness == null) {
                return;
            }
            _moisture = precipitationIntensity > 0 ?
                Mathf.Min(_moisture + (precipitationIntensity * .25f) * Time.deltaTime, 1) :
                Mathf.Max(_moisture - dryingRate * Time.deltaTime, 0);
            _screenSpaceWetness.enabled = precipitationIntensity > VisiblePrecipitationIntensity;
            _screenSpaceWetness.Update(rainIntensity, _moisture);
        }

        static float BlendProgress(DelayedValue value) {
            if (value.IsStable) {
                return 1f;
            }
            return value.Target > 0.5f ? value.Value : 1f - value.Value;
        }
    }
}
