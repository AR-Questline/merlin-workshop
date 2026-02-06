using System;
using System.Runtime.CompilerServices;
using Awaken.TG.Main.Timing;
using Awaken.TG.MVC;
using Awaken.Utility.Collections;
using Awaken.Utility.Debugging;
using Awaken.Utility.Maths;
using Sirenix.OdinInspector;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Serialization;
using ColorUtils = Awaken.Utility.Maths.ColorUtils;
#if UNITY_EDITOR
using UnityEditor.SceneManagement;
using UnityEditor;
#endif

namespace Awaken.TG.Graphics.VFX {
    [ExecuteAlways]
    public abstract class LightsOverride : MonoBehaviour, UnityUpdateProvider.IWithLateUpdateGeneric {
        [InfoBox("$" + nameof(RunInEditModeInfoBoxMessage), InfoMessageType.Info, nameof(IsInEditMode))]
        [SerializeField, Required] internal LightWithOverrides[] lightsWithOverrides = Array.Empty<LightWithOverrides>();
        [SerializeField] protected byte priority = 0;
        [SerializeField, FormerlySerializedAs("intensityMultiplierLerpSpeed")] float lerpSpeedMultiplier = 1f;

#if UNITY_EDITOR
        static event Action EDITOR_OnClearedAllOverrides;
        static event Action EDITOR_ForceClearAllOverrides;
#endif
        protected abstract bool RunUpdateInEditMode { get; }
        string RunInEditModeInfoBoxMessage => RunUpdateInEditMode ? "Supports update in edit mode" : "Does not support update in edit mode";
        bool IsInEditMode => Application.isPlaying == false;

        GameRealTime _gameRealTime;
        bool _startedOverride;

        void OnEnable() {
#if UNITY_EDITOR
            if (IsInEditMode && RunUpdateInEditMode == false) {
                return;
            }
#endif
            Setup();
            UnityUpdateProvider.GetOrCreate().RegisterLateGeneric(this);
        }

        void OnDisable() {
#if UNITY_EDITOR
            if (IsInEditMode && RunUpdateInEditMode == false) {
                return;
            }
#endif
            StopOverride();
            UnityUpdateProvider.GetOrCreate().UnregisterLateGeneric(this);
        }

        public void UnityLateUpdate(float deltaTime) {
#if UNITY_EDITOR
            if (IsInEditMode) {
                if (RunUpdateInEditMode &&
                    ((LightController.EditorPreviewUpdates && EDITOR_IsThisOrUsedLightSelected()) || LightController.EditorPreviewAllUpdates)) {
                    OnLateUpdate(deltaTime);
                } else {
                    StopOverride();
                }
            } else
#endif
            {
                OnLateUpdate(deltaTime);
            }
        }

        public abstract void OnLateUpdate(float deltaTime);

        protected virtual void Setup() {
            bool hasNullLights = false;
            for (int i = 0; i < lightsWithOverrides.Length; i++) {
                var data = lightsWithOverrides[i];
                if (data.light == null) {
                    hasNullLights = true;
                    continue;
                }
                if (data.intensityOverride.useOverride) {
                    try {
                        data.intensityOverride.nativeIntensity = LightUnitUtils.ConvertIntensity(
                            data.light.Light, data.intensityOverride.intensity, data.intensityOverride.lightUnit, LightUnitUtils.GetNativeLightUnit(data.light.Light.type));
                        float daytimeIntensityMultiplier = data.daytimeIntensityMultiplier.useOverride ? data.daytimeIntensityMultiplier.intensityMultiplier : 1;
                        data.intensityOverride.nativeIntensityWithDaytimeMultiplier = LightUnitUtils.ConvertIntensity(
                            data.light.Light, data.intensityOverride.intensity * daytimeIntensityMultiplier, data.intensityOverride.lightUnit, LightUnitUtils.GetNativeLightUnit(data.light.Light.type));
                        lightsWithOverrides[i] = data;
                    } catch (Exception e) {
                        Log.Critical?.Error(e.Message, this);
                    }
                } else {
                    if (data.light.Light) {
                        data.intensityOverride.lightUnit = data.light.Light.lightUnit;
                        lightsWithOverrides[i] = data;
                    }
                }
#if UNITY_EDITOR
                if (IsInEditMode) {
                    data.light.ClearAllOverrides();
                    EDITOR_ResetFlagStartedOverride();
                    EDITOR_OnClearedAllOverrides?.Invoke();
                }
#endif
            }

#if UNITY_EDITOR
            if (IsInEditMode == false)
#endif
            {
                if (hasNullLights) {
                    Log.Important?.Error($"{this.GetType().Name} on gameObject {name} contains null references in {nameof(lightsWithOverrides)} array. Array elements with null references will be removed", this);
                    lightsWithOverrides = lightsWithOverrides.CreateCopyRemovingNulls(static x => x.light == null);
                }
            }

            _gameRealTime = World.Any<GameRealTime>();
#if UNITY_EDITOR
            if (IsInEditMode) {
                EDITOR_OnClearedAllOverrides -= EDITOR_ResetFlagStartedOverride;
                EDITOR_OnClearedAllOverrides += EDITOR_ResetFlagStartedOverride;
                EDITOR_ForceClearAllOverrides -= EDITOR_ForceRemoveOverrides;
                EDITOR_ForceClearAllOverrides += EDITOR_ForceRemoveOverrides;
            }
#endif
        }

        protected void ApplyLightOverrides(float deltaTime, float blendingFactor) {
            int lightsCount = lightsWithOverrides.Length;

            if (!_startedOverride) {
                _startedOverride = true;
                for (int i = 0; i < lightsCount; i++) {
                    var data = lightsWithOverrides[i];
#if UNITY_EDITOR
                    if (data.light == null) {
                        continue;
                    }
                    data.light.EnsureInitializedInEditorMode();
#endif
                    if (data.colorOverride.useOverride) {
                        data.light.StartColorOverride(priority);
                    }
                    if (data.colorTemperatureOverride.useOverride) {
                        data.light.StartColorTemperatureOverride(priority);
                    }
                    if (data.volumetricDimmerOverride.useOverride) {
                        data.light.StartVolumetricDimmerOverride(priority);
                    }
                    if (data.daytimeIntensityMultiplier.useOverride || data.intensityOverride.useOverride) {
                        data.light.StartIntensityOverride(priority);
                    }
                }
            }

            for (int i = 0; i < lightsCount; i++) {
                var data = lightsWithOverrides[i];
#if UNITY_EDITOR
                if (data.light == null) {
                    continue;
                }
#endif
                if (data.colorOverride.useOverride) {
                    var fromColor = data.light.GetColorWithLowerPriority(priority).ToFloat4();
                    var toColor = data.colorOverride.color.ToFloat4();
                    var updatedOverridenColor = ColorUtils.FromFloat4(GetSmoothlyInterpolatedOverrideValue(fromColor, toColor, blendingFactor,
                        fromColor, data.light.ColorWithOverride.ToFloat4(), deltaTime, lerpSpeedMultiplier));
                    data.light.SetColorOverride(updatedOverridenColor, priority);
                }
                if (data.colorTemperatureOverride.useOverride) {
                    var fromTemperature = data.light.GetColorTemperatureWithLowerPriority(priority);
                    var toTemperature = data.colorTemperatureOverride.colorTemperature;
                    var updatedOverridenTemperature = GetSmoothlyInterpolatedOverrideValue(fromTemperature, toTemperature, blendingFactor,
                        fromTemperature, data.light.ColorTemperatureWithOverride, deltaTime, lerpSpeedMultiplier);
                    data.light.SetColorTemperatureOverride(updatedOverridenTemperature, priority);
                }
                if (data.volumetricDimmerOverride.useOverride) {
                    var fromVolumetricDimmer = data.light.GetVolumetricDimmerWithLowerPriority(priority);
                    var toVolumetricDimmer = data.volumetricDimmerOverride.volumetricDimmer;
                    var updatedVolumetricDimmer = GetSmoothlyInterpolatedOverrideValue(fromVolumetricDimmer, toVolumetricDimmer, blendingFactor,
                        fromVolumetricDimmer, data.light.VolumetricDimmerWithOverride, deltaTime, lerpSpeedMultiplier);
                    data.light.SetVolumetricDimmerOverride(updatedVolumetricDimmer, priority);
                }
                bool isMatchingDaytime = false;
                if (data.daytimeIntensityMultiplier.useOverride) {
                    isMatchingDaytime = data.daytimeIntensityMultiplier.applyTime.IsMatching(_gameRealTime);
                }
                float fromIntensity = 1;
                float toIntensity = 2;
                var lowerPriorityOverrideIntensity = data.light.GetIntensityWithLowerPriority(priority);
                if (data.intensityOverride.useOverride) {
                    if (data.daytimeIntensityMultiplier.useOverride) {
                        if (data.daytimeIntensityMultiplier.applyOverrideOnlyAtThisDaytime && !isMatchingDaytime) {
                            fromIntensity = data.intensityOverride.nativeIntensityWithDaytimeMultiplier;
                            toIntensity = lowerPriorityOverrideIntensity;
                        } else {
                            fromIntensity = lowerPriorityOverrideIntensity;
                            toIntensity = isMatchingDaytime ? data.intensityOverride.nativeIntensityWithDaytimeMultiplier : data.intensityOverride.nativeIntensity;
                        }
                    } else {
                        fromIntensity = lowerPriorityOverrideIntensity;
                        toIntensity = data.intensityOverride.nativeIntensity;
                    }
                } else if (data.daytimeIntensityMultiplier.useOverride) {
                    if (isMatchingDaytime) {
                        fromIntensity = lowerPriorityOverrideIntensity;
                        // Only EV100 is non-linear light units, but none of lights have EV100 as native intensity, so it is safe and correct to 
                        // multiply light.intensity by multiplier and use the result as is.
                        toIntensity = lowerPriorityOverrideIntensity * data.daytimeIntensityMultiplier.intensityMultiplier;
                    } else {
                        // If daytime multiplier was used but now time is not matching with applyTime
                        fromIntensity = lowerPriorityOverrideIntensity * data.daytimeIntensityMultiplier.intensityMultiplier;
                        toIntensity = lowerPriorityOverrideIntensity;
                    }
                }

                if (data.daytimeIntensityMultiplier.useOverride || data.intensityOverride.useOverride) {
                    var updatedOverridenIntensity = GetSmoothlyInterpolatedOverrideValue(fromIntensity, toIntensity, blendingFactor,
                        lowerPriorityOverrideIntensity, data.light.IntensityWithOverride, deltaTime, lerpSpeedMultiplier);
                    data.light.SetIntensityOverride(updatedOverridenIntensity, priority);
                }
            }
        }

        protected void StopOverride() {
            if (!_startedOverride) {
                return;
            }
            _startedOverride = false;
            int lightsCount = lightsWithOverrides.Length;
            for (int i = 0; i < lightsCount; i++) {
                var data = lightsWithOverrides[i];
                if (data.light == null) {
                    return;
                }
                if (data.colorOverride.useOverride) {
                    data.light.StopColorOverride(priority);
                }
                if (data.colorTemperatureOverride.useOverride) {
                    data.light.StopColorTemperatureOverride(priority);
                }
                if (data.volumetricDimmerOverride.useOverride) {
                    data.light.StopVolumetricDimmerOverride(priority);
                }
                if (data.intensityOverride.useOverride || data.daytimeIntensityMultiplier.useOverride) {
                    data.light.StopIntensityOverride(priority);
                }
            }
        }

        static float GetSmoothlyInterpolatedOverrideValue(
            float fromValue, float toValue, float blendingFactor, float lowerPriorityOverridenValue, float overridenValue, float deltaTime, float lerpSpeed) {
            var currentOverridenValueWithoutBlending = blendingFactor != 0
                ? mathExt.FindLerpEndValue(lowerPriorityOverridenValue, blendingFactor, overridenValue)
                : lowerPriorityOverridenValue;
            var tValue = math.unlerp(fromValue, toValue, currentOverridenValueWithoutBlending);
            tValue = math.clamp(tValue + (lerpSpeed * deltaTime), 0, 1);
            var thisFrameOverrideIntensity = math.lerp(fromValue, toValue, tValue);
            var resultingOverridenValue = math.lerp(lowerPriorityOverridenValue, thisFrameOverrideIntensity, blendingFactor);
            return resultingOverridenValue;
        }

        static float4 GetSmoothlyInterpolatedOverrideValue(
            float4 fromValue, float4 toValue, float blendingFactor, float4 lowerPriorityOverridenValue, float4 overridenValue, float deltaTime, float lerpSpeed) {
            var currentOverridenValueWithoutBlending = blendingFactor != 0
                ? mathExt.FindLerpEndValue(lowerPriorityOverridenValue, blendingFactor, overridenValue)
                : lowerPriorityOverridenValue;
            var tValue = math.unlerp(fromValue, toValue, currentOverridenValueWithoutBlending);
            tValue = math.clamp(tValue + (lerpSpeed * deltaTime), 0, 1);
            var thisFrameOverrideIntensity = math.lerp(fromValue, toValue, tValue);
            var resultingOverridenValue = math.lerp(lowerPriorityOverridenValue, thisFrameOverrideIntensity, blendingFactor);
            return resultingOverridenValue;
        }

#if UNITY_EDITOR
        void OnValidate() {
            if (RunUpdateInEditMode) {
                Setup();
            }
        }

        bool EDITOR_IsThisOrUsedLightSelected() {
            var selectedGameObjects = Selection.gameObjects;
            if (selectedGameObjects.Contains(this.gameObject)) {
                return true;
            }
            foreach (var lightWithOverride in lightsWithOverrides) {
                if (lightWithOverride.light != null && selectedGameObjects.Contains(lightWithOverride.light.gameObject)) {
                    return true;
                }
            }
            return false;
        }

        void EDITOR_ResetFlagStartedOverride() {
            _startedOverride = false;
        }

        void EDITOR_ForceRemoveOverrides() {
            for (int i = 0; i < lightsWithOverrides.Length; i++) {
                var data = lightsWithOverrides[i];
                if (data.light == null) {
                    continue;
                }
                data.light.ClearAllOverrides();
            }
            EDITOR_ResetFlagStartedOverride();
        }
        
        [InitializeOnLoadMethod]
        static void EditorOnInitialize()
        {
            EditorSceneManager.sceneSaving -= OnBeforeSceneSaved;
            EditorSceneManager.sceneSaving += OnBeforeSceneSaved;
        }

        static void OnBeforeSceneSaved(UnityEngine.SceneManagement.Scene scene, string path)
        {
            EDITOR_ForceClearAllOverrides?.Invoke();
        }
#endif
    }

    [Serializable]
    struct LightWithOverrides {
        public LightWithOverride light;

        [LabelText("filter"), Toggle(nameof(ColorOverride.useOverride))]
        public ColorOverride colorOverride;

        [LabelText("temperature"), Toggle(nameof(TemperatureOverride.useOverride))]
        public TemperatureOverride colorTemperatureOverride;

        [SerializeField, LabelText("intensity"), Toggle(nameof(IntensityOverride.useOverride))]
        public IntensityOverride intensityOverride;

        [SerializeField, LabelText("daytime intensity multiplier"), Toggle(nameof(IntensityOverride.useOverride))]
        public DaytimeIntensityMultiplierOverride daytimeIntensityMultiplier;

        [SerializeField, LabelText("volumetrics multiplier"), Toggle(nameof(VolumetricDimmerOverride.useOverride))]
        public VolumetricDimmerOverride volumetricDimmerOverride;
    }

    [Serializable]
    struct ColorOverride {
        public bool useOverride;
        public Color color;
    }

    [Serializable]
    struct TemperatureOverride {
        public bool useOverride;
        public float colorTemperature;
    }

    [Serializable]
    struct IntensityOverride {
        public bool useOverride;
        public LightUnit lightUnit;
        public float intensity;
        [HideInInspector] public float nativeIntensity;
        [HideInInspector] public float nativeIntensityWithDaytimeMultiplier;
    }

    [Serializable]
    struct DaytimeIntensityMultiplierOverride {
        public bool useOverride;
        public bool applyOverrideOnlyAtThisDaytime;
        public ApplyTime applyTime;
        public float intensityMultiplier;
    }

    [Serializable]
    struct VolumetricDimmerOverride {
        public bool useOverride;
        [LabelText("Volumetrics Multiplier")] public float volumetricDimmer;
    }

    public enum ApplyTime : byte {
        Always = 0,
        Day = 1,
        Night = 2
    }

    static class ApplyTimeExtensions {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsMatching(this ApplyTime applyTime, GameRealTime gameRealTime) {
            return applyTime == ApplyTime.Always ||
                   (gameRealTime != null &&
                    ((gameRealTime.WeatherTime.IsDay && applyTime == ApplyTime.Day) ||
                     (!gameRealTime.WeatherTime.IsDay && applyTime == ApplyTime.Night)));
        }
    }
}