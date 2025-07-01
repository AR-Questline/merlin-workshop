using System;
using System.Runtime.CompilerServices;
using Awaken.TG.Main.Grounds.CullingGroupSystem;
using Awaken.TG.Main.Grounds.CullingGroupSystem.CullingGroups;
using Awaken.TG.Main.Locations.Setup;
using Awaken.TG.Main.Timing;
using Awaken.TG.MVC;
using Awaken.Utility.Collections;
using Awaken.Utility.Debugging;
using Awaken.Utility.GameObjects;
using UnityEngine;
using UnityEngine.Rendering.HighDefinition;
using Sirenix.OdinInspector;
using Unity.Mathematics;
using UnityEngine.Rendering;
using UnityEngine.Serialization;

namespace Awaken.TG.Graphics.VFX {
    [ExecuteAlways]
    [DefaultExecutionOrder(15)]
    [RequireComponent(typeof(Light), typeof(HDAdditionalLightData))]
    public class LightController : MonoBehaviour, ICullingSystemRegistree, IVFXOnStopEffects {
        public const string PrefsKey = "LightController: ";
        static bool s_editorPreviewUpdates, s_editorPreviewAllUpdates;
        const float MinFrequency = 0.1f;
        const float MaxFrequency = 20f;

        const float MinLoopTime = 0.01f;

        // When quite some time elapsed and perlin noise y coord (dependent on time) is big enough, perlin noise range shrinks down to smaller range of output values.
        const float PerlinNoiseMin = 0.05f;

        const float PerlinNoiseMax = 0.9f;

        // This value needed to start sampling perlin noise with some offset to start far enough to be already in smaller range of output values 
        const float PerlinNoiseRangeStartOffset = 5000;
        const float PerlinNoiseXRangeMax = 3000;
        const float PerlinNoiseYRangeMax = 10000;
        const float CurvesTimeMin = 0.01f;

        /// <summary>
        /// If Enabled will cause changes to scene that should be discarded
        /// </summary>
        public static bool EditorPreviewUpdates {
            get => Application.isPlaying || s_editorPreviewUpdates;
            set => s_editorPreviewUpdates = value;
        }

        public static bool EditorPreviewAllUpdates {
            get => Application.isPlaying || s_editorPreviewAllUpdates;
            set => s_editorPreviewAllUpdates = value;
        }

        public Vector3 Coords => transform.position;

        [SerializeField, Toggle(nameof(ColorToggleObject.useColor)), BoxGroup("Setup")]
        ColorToggleObject color = new ColorToggleObject();

        [SerializeField, Toggle(nameof(IntensityToggleObject.useIntensity)), BoxGroup("Setup")]
        IntensityToggleObject intensity;

        [SerializeField, Toggle(nameof(RangeToggleObject.useRange)), BoxGroup("Setup")]
        RangeToggleObject range = new RangeToggleObject();

        [SerializeField, Toggle(nameof(DayNightCycleToggleObject.useDayNightCycle)), BoxGroup("Setup")]
        DayNightCycleToggleObject dayNightCycle = new DayNightCycleToggleObject();

        [SerializeField, Toggle(nameof(OptimizeToggleObject.useOptimize)), BoxGroup("Setup")]
        OptimizeToggleObject optimize = new OptimizeToggleObject();
        
        [SerializeField, Toggle(nameof(FadeEffectsToggleObject.useFadeEffects)), BoxGroup("Setup")]
        FadeEffectsToggleObject fadeEffects = new FadeEffectsToggleObject();

        [SerializeField, LabelText("Time and Noise"), BoxGroup("Setup")]
        TimeAndNoiseObject timeAndNoise = TimeAndNoiseObject.Default;

        [SerializeField, BoxGroup("Setup")]
        LightSize _lightSize = LightSize.Medium;

        [SerializeField] bool forceStaticIfInitiallyOnScene = true;
        [SerializeField, HideInInspector] NativeIntensityData bakedNativeIntensity;
        [SerializeField, HideInInspector] bool bakedIsStatic;
        HDAdditionalLightData _lightData;
        Light _light;
        float _timeMult, _timeLoopValue, _curvesTime;
        uint _randomSeed;
        float _fadeValue = 1.0f;
        float _targetFadeValue = 1.0f;
        float _currentFadeSpeed, _fadeInSpeed, _fadeOutSpeed;
        bool _fading;
        GameRealTime _gameRealTime;
        Registree _registree;
        UpdateType _currentUpdateType;
#if UNITY_EDITOR
        [ShowInInspector, ReadOnly, BoxGroup("Setup")]
        UpdateType CurrentUpdateType => _currentUpdateType;
#endif
#if UNITY_EDITOR || AR_DEBUG
        Vector3 _DEBUG_staticStartPos;
#endif

        void Awake() {
            EnsureCorrectStaticStatusInEditorPlaymode();
            TryMakeStatic();
        }

        void Start() {
            ValidateAndInitialize();
            EnsureBakedIntensity();
            StartFadingInOnEnable();
            UpdateOnce();
        }

        void OnEnable() {
#if UNITY_EDITOR
            if (Application.isPlaying == false) {
                _currentUpdateType = UpdateType.ActiveUpdate;
                UnityUpdateProvider.GetOrCreate().RegisterLightControllerActive(this);
            } else
#endif
            {
                CullingSystemRegistrator.Register(this);
                _currentUpdateType = bakedIsStatic ? UpdateType.CulledButStaticSoNoUpdate : UpdateType.CulledUpdate;
                if (bakedIsStatic == false) {
                    UnityUpdateProvider.GetOrCreate().RegisterLightControllerCulled(this);
                }
#if UNITY_EDITOR || AR_DEBUG
                else {
                    UnityUpdateProvider.GetOrCreate().DEBUG_RegisterLightControllerCulledStatic(this);
                }
#endif
                if (_light != null) {
                    StartFadingInOnEnable();
                }
            }
        }

        void OnDisable() {
            switch (_currentUpdateType) {
                case UpdateType.ActiveUpdate:
                    UnityUpdateProvider.GetOrCreate().UnregisterLightControllerActive(this);
                    break;
                case UpdateType.CulledUpdate:
                    UnityUpdateProvider.GetOrCreate().UnregisterLightControllerCulled(this);
                    break;
#if UNITY_EDITOR || AR_DEBUG
                case UpdateType.CulledButStaticSoNoUpdate:
                    UnityUpdateProvider.GetOrCreate().DEBUG_UnregisterLightControllerCulledStatic(this);
                    break;
#endif
            }

            _currentUpdateType = UpdateType.None;
#if UNITY_EDITOR
            if (UnityEditor.EditorApplication.isPlaying)
#endif
            {
                CullingSystemRegistrator.Unregister(this);
                _registree = null;
            }
        }

#if UNITY_EDITOR
        public void OnValidate() {
            if (this == null || this.gameObject == null) {
                return;
            }

            bool isSelected = UnityEditor.Selection.activeTransform == transform;
            if (_lightData == null || !isSelected) {
                return;
            }
            ValidateAndInitialize();
            UpdateOnce();
            UpdateEveryFrame();
            BakeNativeIntensity();
        }

        void Reset() {
            intensity.intensityCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);
            range.rangeCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);
            range.rangeMultiplier = 1;
            dayNightCycle.intensityByDayTime = new(new Keyframe(0f, 1f), new Keyframe(.25f, 1f), new Keyframe(.25f, 0f),
                new Keyframe(.75f, 0f), new Keyframe(.75f, 1f), new Keyframe(1f, 1f));
            optimize.useOptimize = true;
            optimize.lightFadeDistance = 256.0f;
            optimize.shadowFadeDistance = 128.0f;
            timeAndNoise = TimeAndNoiseObject.Default;
        }
#endif

        void EnsureBakedIntensity() {
            if (bakedNativeIntensity.Equals(default)) {
                BakeNativeIntensity();
            }
        }

        void EnsureNoMissingValues() {
            if (timeAndNoise.useTimeLoop && timeAndNoise.loopTime == 0) {
                timeAndNoise.loopTime = timeAndNoise.timeInverseMultiplier;
            }
            if (timeAndNoise.curvesTime == 0) {
                timeAndNoise.curvesTime = timeAndNoise.timeInverseMultiplier;
            }
        }

        public void BakeNativeIntensity() {
            bakedNativeIntensity = GetNativeIntensityData();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        NativeIntensityData GetNativeIntensityData() {
            if (intensity.useIntensity) {
                return GetNativeIntensityForUseIntensity(GetLightUnit(intensity.intensityUnits));
            }

            if (dayNightCycle.useDayNightCycle) {
                return GetNativeIntensityForUseDayNightCycle(dayNightCycle.unit);
            }

            return default;
        }

        [System.Diagnostics.Conditional("UNITY_EDITOR")]
        void EnsureCorrectStaticStatusInEditorPlaymode() {
#if UNITY_EDITOR
            if (forceStaticIfInitiallyOnScene && bakedIsStatic == false && Application.isPlaying) {
                // In editor playmode, OnEnable is called before LightControllersBaker.
                // LightControllerBaker makes static all initially placed in scene LightControllers if forceStaticIfInitiallyOnScene (as for April 2nd 2025).
                bool isInitiallyPlacedOnScene = gameObject.scene.isLoaded == false;
                if (gameObject.isStatic || isInitiallyPlacedOnScene) {
                    bakedIsStatic = true;
                }
            }
#endif
        }

        void TryMakeStatic() {
#if UNITY_EDITOR
            if (Application.isPlaying == false) {
                return;
            }
#endif
            if (gameObject.scene.isLoaded && IsRuntimeSpawnedButStatic()) {
                bakedIsStatic = true;
            }
        }

        bool IsRuntimeSpawnedButStatic() {
            var locationSpec = gameObject.GetComponentInParent<LocationSpec>();
            if (locationSpec != null) {
                return locationSpec.IsNonMovable
                       || locationSpec.prefabReference.IsSet == false 
                       || locationSpec.IsHidableStatic;
            }
            return false;
        }

        void ValidateAndInitialize() {
            EnsureNoMissingValues();
            EnsureValidSetup();
            // In old code timeMultiplier was used everywhere with expensive divide operation. Added to improve performance without breaking already set values
            _timeMult = 1 / timeAndNoise.timeInverseMultiplier;
            _timeLoopValue = timeAndNoise.useTimeLoop ? timeAndNoise.loopTime : float.PositiveInfinity;
            _curvesTime = timeAndNoise.useTimeLoop ? timeAndNoise.loopTime : timeAndNoise.curvesTime;
            _randomSeed = GenerateRandomSeed();
            _fadeInSpeed = 1f / math.max(fadeEffects.fadeInTime, 0.001f);
            _fadeOutSpeed = 1f / math.max(fadeEffects.fadeOutTime, 0.001f);
#if UNITY_EDITOR || AR_DEBUG
            _DEBUG_staticStartPos = gameObject.isStatic || bakedIsStatic ? transform.position : Vector3.zero;
#endif

            EnsureLightUnitMatchesSettings();
            Optimizations();
        }

        uint GenerateRandomSeed() {
            switch (timeAndNoise.seedOption) {
                case SeedOption.Unique:
                    return GenerateUniqueSeed(gameObject.GetHashCode());
                case SeedOption.Shared:
                    return timeAndNoise.noiseSeedSO != null ? timeAndNoise.noiseSeedSO.seed : GenerateUniqueSeed(gameObject.GetHashCode());
                case SeedOption.Inherit:
                    return timeAndNoise.lightToCopySeedFrom != null ? timeAndNoise.lightToCopySeedFrom.GenerateRandomSeed() : GenerateUniqueSeed(gameObject.GetHashCode());
                case SeedOption.Specific:
                    return timeAndNoise.seed != 0 ? timeAndNoise.seed : GenerateUniqueSeed(gameObject.GetHashCode());
                default:
                    Log.Important?.Error($"Option {timeAndNoise.seedOption} is not handled");
                    return GenerateUniqueSeed(gameObject.GetHashCode());
            }

            static uint GenerateUniqueSeed(int instanceId) => math.hash(new uint2((uint)instanceId));
        }

#if UNITY_EDITOR
        public void EditorUpdate() {
            if (EditorPreviewAllUpdates || UnityEditor.Selection.activeTransform == transform) {
                ActiveLightUpdate();
            }
        }

        void StopUpdateInEditor() {
            if (_currentUpdateType == UpdateType.ActiveUpdate) {
                _currentUpdateType = UpdateType.None;
                UnityUpdateProvider.GetOrCreate().UnregisterLightControllerActive(this);
            }
        }
#endif
        public void ActiveLightUpdate() {
            HandleDayNightCycle();
            HandleFadeEffects();
            UpdateEveryFrame();
            UpdatePositionForCulling();
#if UNITY_EDITOR || AR_DEBUG
            if (bakedIsStatic) {
                DEBUG_CheckAndFixIfStaticIsMoving();
            }
#endif
        }

        public void CulledLightUpdate() {
            UpdatePositionForCulling();
        }

#if UNITY_EDITOR || AR_DEBUG
        public void DEBUG_CheckAndFixIfStaticIsMoving() {
            if ((transform.position - _DEBUG_staticStartPos).sqrMagnitude > 0.01f) {
                Log.Important?.Error($"LightController {gameObject.PathInSceneHierarchy(true)} is marked as static but it was moved", this);
                this.enabled = false;

                bakedIsStatic = false;
                gameObject.isStatic = false;

                this.enabled = true;
            }
        }
#endif

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        float GetCurvesTime() {
            return ((Time.time + timeAndNoise.timeOffset) * _timeMult % _timeLoopValue) / _curvesTime;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        float GetDynamicRandomTime() {
            return (Time.time + timeAndNoise.timeOffset) * _timeMult;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        void HandleDayNightCycle() {
            if (dayNightCycle.useDayNightCycle) {
                float dayTime;
                NativeIntensityData nativeIntensity = bakedNativeIntensity;
#if UNITY_EDITOR
                if (Application.isPlaying == false) {
                    dayTime = dayNightCycle.testDayTimeValue;
                    nativeIntensity = GetNativeIntensityData();
                } else
#endif
                {
                    dayTime = _gameRealTime.WeatherTime.DayTime;
                }
                _light.intensity = nativeIntensity.intensityCurve.Evaluate(dayTime) * _fadeValue;
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        void HandleFadeEffects() {
            if (!_fading) {
                return;
            }

            float remaining = _targetFadeValue - _fadeValue;
            float sign = math.sign(remaining);
            float remainingAbs = remaining * sign;
            float step = _currentFadeSpeed * Time.deltaTime;
            _fadeValue += math.min(step, remainingAbs) * sign;

            _fading = remainingAbs > math.EPSILON;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        void UpdatePositionForCulling() {
            if (!bakedIsStatic) {
                _registree?.UpdateOwnPosition();
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        void Optimizations() {
            if (optimize.useOptimize) {
                _lightData.shadowFadeDistance = optimize.shadowFadeDistance;
                _lightData.fadeDistance = optimize.lightFadeDistance;
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        void StartFadingInOnEnable() {
#if UNITY_EDITOR
            if (!Application.isPlaying) {
                return;
            }
#endif
            if (fadeEffects.useFadeEffects && fadeEffects.fadeInOnEnable) {
                _fading = true;
                _targetFadeValue = 1.0f;
                _currentFadeSpeed = _fadeInSpeed;
                
                _fadeValue = 0.0f;
                _light.intensity = 0.0f;
            }
        }

        public Registree GetRegistree() {
            EnsureCorrectStaticStatusInEditorPlaymode();
            var registree = Registree.ConstructFor<LightControllerCullingGroup>(this).Build();
            bool saveRegistree = !bakedIsStatic;
#if UNITY_EDITOR || AR_DEBUG
            saveRegistree = true;
#endif
            if (saveRegistree) {
                _registree = registree;
            }

            return registree;
        }

        public void CullingSystemBandUpdated(int newDistanceBand) {
            bool isCulled = !LightControllerCullingGroup.IsLightControllerUpdateBand(newDistanceBand, _lightSize);
            bool isCulledUpdate = (_currentUpdateType == UpdateType.CulledUpdate) | (_currentUpdateType == UpdateType.CulledButStaticSoNoUpdate);
            if (!isCulledUpdate & isCulled) {
                _currentUpdateType = bakedIsStatic ? UpdateType.CulledButStaticSoNoUpdate : UpdateType.CulledUpdate;
                var updateProvider = UnityUpdateProvider.GetOrCreate();
                updateProvider.UnregisterLightControllerActive(this);
                if (!bakedIsStatic) {
                    updateProvider.RegisterLightControllerCulled(this);
                }
#if UNITY_EDITOR || AR_DEBUG
                else {
                    updateProvider.DEBUG_RegisterLightControllerCulledStatic(this);
                }
#endif
            } else {
                bool hasAllDataForActiveUpdate = (dayNightCycle.useDayNightCycle == false) || ((_gameRealTime ??= World.Any<GameRealTime>()) != null);
                if ((_currentUpdateType != UpdateType.ActiveUpdate) & !isCulled & hasAllDataForActiveUpdate) {
                    _currentUpdateType = UpdateType.ActiveUpdate;
                    var updateProvider = UnityUpdateProvider.GetOrCreate();
                    if (!bakedIsStatic) {
                        updateProvider.UnregisterLightControllerCulled(this);
                    }
#if UNITY_EDITOR || AR_DEBUG
                    else {
                        updateProvider.DEBUG_UnregisterLightControllerCulledStatic(this);
                    }
#endif
                    updateProvider.RegisterLightControllerActive(this);
                }
            }
        }
        
        public void VFXStopped() {
            if (fadeEffects.useFadeEffects && fadeEffects.fadeOutOnVfxStop) {
                _fading = true;
                _targetFadeValue = 0.0f;
                _currentFadeSpeed = _fadeOutSpeed;
            }
        }

        public void UpdateOnce() {
#if UNITY_EDITOR
            if (Application.isPlaying == false && !EditorPreviewUpdates) return;
#endif
            var nativeIntensity = bakedNativeIntensity;
#if UNITY_EDITOR
            if (Application.isPlaying == false) {
                nativeIntensity = GetNativeIntensityData();
            }
#endif
            var random = new Unity.Mathematics.Random(_randomSeed);
            // Intensity
            if (intensity.useIntensity & (intensity.intensityType == ModType.Static)) {
                _light.intensity = random.NextFloat(nativeIntensity.intensityMinOrBaseIntensity, nativeIntensity.intensityMax);
            }

            if (range.useRange & (range.rangeType == ModType.Static)) {
                _lightData.range = random.NextFloat(range.rangeStaticMin, range.rangeStaticMax);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        void UpdateEveryFrame() {
            // Color
            if (color.useColor) {
                _lightData.color = color.colorGradient.Evaluate(GetCurvesTime());
            }

            // Intensity
            if (intensity.useIntensity & (intensity.intensityType != ModType.Static)) {
                var nativeIntensity = bakedNativeIntensity;
#if UNITY_EDITOR
                if (Application.isPlaying == false) {
                    nativeIntensity = GetNativeIntensityData();
                }
#endif
                var baseIntensity = intensity.intensityType switch {
                    ModType.Curve => nativeIntensity.intensityCurve.Evaluate(GetCurvesTime()),
                    _ => math.lerp(
                        nativeIntensity.intensityMinOrBaseIntensity, nativeIntensity.intensityMax,
                        GetPerlinNoiseValue01(GetDynamicRandomTime(), timeAndNoise.noiseFrequency, _timeLoopValue, _randomSeed))
                };

                _light.intensity = baseIntensity * _fadeValue;
            }

            // Range
            if (range.useRange & (range.rangeType != ModType.Static)) {
                _lightData.range = range.rangeType switch {
                    ModType.Curve => range.rangeCurve.Evaluate(GetCurvesTime()) * range.rangeMultiplier,
                    _ => math.lerp(
                        range.rangeDynamicMin, range.rangeDynamicMax,
                        GetPerlinNoiseValue01(GetDynamicRandomTime(), timeAndNoise.noiseFrequency, _timeLoopValue, _randomSeed))
                };
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        float ConvertToNativeIntensity(float intensity, Light light, LightUnit lightUnit) {
            return LightUnitUtils.ConvertIntensity(light, intensity, lightUnit, LightUnitUtils.GetNativeLightUnit(light.type));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        void EnsureLightUnitMatchesSettings() {
            if (intensity.useIntensity) {
                var targetUnit = GetLightUnit(intensity.intensityUnits);
                if (_light.lightUnit != targetUnit) {
                    _light.lightUnit = targetUnit;
                }
            } else if (dayNightCycle.useDayNightCycle) {
                if (_light.lightUnit != dayNightCycle.unit) {
                    _light.lightUnit = dayNightCycle.unit;
                }
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        NativeIntensityData GetNativeIntensityForUseIntensity(LightUnit lightUnit) {
            NativeIntensityData nativeIntensity = default;
            var light = GetComponent<Light>();
            switch (intensity.intensityType) {
                case ModType.Static:
                    nativeIntensity.intensityMinOrBaseIntensity = ConvertToNativeIntensity(intensity.intensityStaticMin, light, lightUnit);
                    nativeIntensity.intensityMax = ConvertToNativeIntensity(intensity.intensityStaticMax, light, lightUnit);
                    break;
                case ModType.Dynamic:
                    nativeIntensity.intensityMinOrBaseIntensity = ConvertToNativeIntensity(intensity.intensityDynamicMin, light, lightUnit);
                    nativeIntensity.intensityMax = ConvertToNativeIntensity(intensity.intensityDynamicMax, light, lightUnit);
                    break;
                case ModType.Curve:
                    nativeIntensity.intensityCurve = ConvertToNativeIntensityCurve(intensity.intensityCurve, light, lightUnit);
                    break;
                default:
                    throw new NotImplementedException($"Case not implemented for type {intensity.intensityType}");
            }

            return nativeIntensity;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        NativeIntensityData GetNativeIntensityForUseDayNightCycle(LightUnit lightUnit) {
            var light = GetComponent<Light>();
            return new NativeIntensityData() {
                intensityCurve = ConvertToNativeIntensityCurve(dayNightCycle.intensityByDayTime, light, lightUnit, dayNightCycle.baseIntensity)
            };
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        AnimationCurve ConvertToNativeIntensityCurve(AnimationCurve curve, Light light, LightUnit lightUnit, float initialCurveMultiplier = 1) {
            var nativeLightUnit = LightUnitUtils.GetNativeLightUnit(light.type);
            var keyframes = curve.keys.CreateCopy();
            int keysCount = keyframes.Length;

            for (int i = 0; i < keysCount; i++) {
                var keyframe = keyframes[i];
                keyframe.value = LightUnitUtils.ConvertIntensity(light, keyframe.value * initialCurveMultiplier, lightUnit, nativeLightUnit);
                keyframes[i] = keyframe;
            }

            return new AnimationCurve(keyframes);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        void EnsureValidSetup() {
            if (_light == null) {
                _light = GetComponent<Light>();
            }

            if (_lightData == null) {
                _lightData = GetComponent<HDAdditionalLightData>();
            }

            if (intensity.useIntensity) {
                var lightUnit = GetLightUnit(intensity.intensityUnits);
                var lightType = _light.type;
                var nativeLightUnit = LightUnitUtils.GetNativeLightUnit(_light.type);
                if (LightUnitUtils.IsLightUnitSupported(lightType, lightUnit) == false) {
                    Log.Important?.Error($"Light unit {lightUnit} is not supported for light type {lightType}. Changing to native light unit {nativeLightUnit}", this);
                    intensity.intensityUnits = GetIntensityUnit(nativeLightUnit);
#if UNITY_EDITOR
                    if (Application.isPlaying == false) {
                        UnityEditor.EditorUtility.SetDirty(this);
                    }
#endif
                }

                if (!LightUnitUtils.IsLightUnitSupported(lightType, lightUnit) || !LightUnitUtils.IsLightUnitSupported(lightType, nativeLightUnit)) {
                    Log.Important?.Error($"Converting from unit {lightUnit} to native light unit {lightType} is undefined for light type {lightType}. Changing to native light unit {nativeLightUnit}", this);
                    intensity.intensityUnits = GetIntensityUnit(nativeLightUnit);
#if UNITY_EDITOR
                    if (Application.isPlaying == false) {
                        UnityEditor.EditorUtility.SetDirty(this);
                    }
#endif
                }

                if (dayNightCycle.useDayNightCycle) {
                    Log.Important?.Error($"Cannot use both {nameof(intensity)} and {nameof(dayNightCycle)} at the same time", this);
                    dayNightCycle.useDayNightCycle = false;
#if UNITY_EDITOR
                    if (Application.isPlaying == false) {
                        UnityEditor.EditorUtility.SetDirty(this);
                    }
#endif
                }
            }
            if (timeAndNoise.useTimeLoop == false && timeAndNoise.curvesTime <= 0) {
                Log.Important?.Error($"{nameof(timeAndNoise.curvesTime)} cannot be 0 or less. Setting to {CurvesTimeMin}");
                timeAndNoise.curvesTime = CurvesTimeMin;
            }
            if (timeAndNoise.seedOption == SeedOption.Specific && timeAndNoise.seed == 0) {
                Log.Important?.Error($"Seed cannot be 0");
                timeAndNoise.seed = GenerateUniqueSeedBasedOnCurrentTime();
            }
            if (timeAndNoise.timeInverseMultiplier == 0) {
                Log.Important?.Error($"{nameof(timeAndNoise.timeInverseMultiplier)} cannot be 0. Setting to 1");
                timeAndNoise.timeInverseMultiplier = 1;
            }
        }

        static uint GenerateUniqueSeedBasedOnCurrentTime() {
            return math.hash(new int2((int)(Time.timeAsDouble * 996571), (int)(Time.deltaTime * 998951)));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        LightUnit GetLightUnit(IntensityUnit intensityUnit) {
            return intensityUnit switch {
                IntensityUnit.Lumen => LightUnit.Lumen,
                IntensityUnit.Candela => LightUnit.Candela,
                IntensityUnit.Lux => LightUnit.Lux,
                IntensityUnit.Ev100 => LightUnit.Ev100,
                _ => throw new ArgumentOutOfRangeException()
            };
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        IntensityUnit GetIntensityUnit(LightUnit lightUnit) {
            return lightUnit switch {
                LightUnit.Ev100 => IntensityUnit.Ev100,
                LightUnit.Lumen => IntensityUnit.Lumen,
                LightUnit.Lux => IntensityUnit.Lux,
                LightUnit.Candela => IntensityUnit.Candela,
                _ => throw new ArgumentOutOfRangeException()
            };
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        float GetPerlinNoiseValue01(float time, float noiseFrequency, float loopTime, uint seed) {
            loopTime = math.max(MinLoopTime, loopTime);
            if (loopTime != float.PositiveInfinity) {
                var doubledLoopTime = loopTime * 2;
                var timeElapsedRanged = time % doubledLoopTime;
                var timeLoopOffset = timeElapsedRanged > loopTime ? doubledLoopTime : 0;
                // bounces time from 0 to loop time and back
                time = math.abs(timeLoopOffset - timeElapsedRanged);
            }
            var perlinNoiseX = (math.hash(new uint2(seed, seed)) % PerlinNoiseXRangeMax) + PerlinNoiseRangeStartOffset;
            var perlinNoiseY = (time % math.min(loopTime, PerlinNoiseYRangeMax) * noiseFrequency + PerlinNoiseRangeStartOffset);
            var perlinNoise = Mathf.PerlinNoise(perlinNoiseX, perlinNoiseY);
            var perlinNoise01 = math.clamp(math.unlerp(PerlinNoiseMin, PerlinNoiseMax, perlinNoise), 0f, 1f);
            return perlinNoise01;
        }

        // === EDITOR
#if UNITY_EDITOR
        void OnDrawGizmos() {
            var styleBold = new GUIStyle {
                fontStyle = FontStyle.Bold,
                fontSize = 12,
                normal = { textColor = Color.white },
                richText = true
            };

            var currentDrawingSceneView = UnityEditor.SceneView.currentDrawingSceneView;
            if (currentDrawingSceneView == null) return;

            Vector3 labelPosition = transform.position + Vector3.down * .1f + Vector3.right * .1f;

            if ((transform.position - currentDrawingSceneView.camera.transform.position).sqrMagnitude < 250 && _light != null && _light.enabled) {
                LightUnit currentLightUnit = _light.lightUnit;
                float lightIntensity = _light.intensity;

                float intensityInLumen = LightUnitUtils.ConvertIntensity(_light, lightIntensity, LightUnit.Candela, LightUnit.Lumen);
                float intensityInLux = LightUnitUtils.ConvertIntensity(_light, lightIntensity, LightUnit.Candela, LightUnit.Lux);
                float intensityInEv100 = LightUnitUtils.ConvertIntensity(_light, lightIntensity, LightUnit.Candela, LightUnit.Ev100);

                float intensityAfterConversion = currentLightUnit switch {
                    LightUnit.Lumen => intensityInLumen,
                    LightUnit.Lux => intensityInLux,
                    LightUnit.Ev100 => intensityInEv100,
                    _ => lightIntensity
                };

                string intensityValue = intensity.useIntensity
                    ? intensity.intensityType == ModType.Static
                        ? $"<color=#00D990>{intensity.intensityStaticMin:F2}-{intensity.intensityStaticMax:F2} [{intensity.intensityType}]</color>"
                        : $"<color=#00D990>{intensity.intensityDynamicMin:F2}-{intensity.intensityDynamicMax:F2} [{intensity.intensityType}]</color>"
                    : $"<color=#0066DB>{intensityAfterConversion:F2} {_light.lightUnit}</color>";

                string rangeValue = range.useRange
                    ? range.rangeType == ModType.Static
                        ? $"<color=#00D990>{range.rangeStaticMin:F2}-{range.rangeStaticMax:F2} [{range.rangeType}]</color>"
                        : $"<color=#00D990>{range.rangeDynamicMin:F2}-{range.rangeDynamicMax:F2} [{range.rangeType}]</color>"
                    : $"<color=#0066DB>{_light.range:F2}</color>";

                string timeValue = timeAndNoise.useTimeLoop
                    ? $"Loop time: <color=#00D990>{timeAndNoise.loopTime}s </color> Loop: <color=#00D990>True</color>"
                    : $"Curves time: <color=#00D990>{timeAndNoise.curvesTime}s </color> Loop: <color=#404040>False</color>";

                string text =
                    $"Type: <color=#0066DB>{_light.type}</color>" +
                    $"\nIntensity: {intensityValue}" +
                    $"\nRange: {rangeValue}" +
                    $"\nShape: <color=#0066DB>{_lightData.shapeRadius}</color>" +
                    $"\nShadows: <color={(_light.shadows == LightShadows.None ? "#404040" : "#FF0000")}>{_light.shadows}</color>" +
                    $"\n{timeValue}";

                UnityEditor.Handles.Label(labelPosition, text, styleBold);
            }
        }

        void OnDrawGizmosSelected() {
            UnityEditor.Handles.RadiusHandle(Quaternion.identity, gameObject.transform.position, optimize.shadowFadeDistance);
            UnityEditor.Handles.color = Color.red;
            UnityEditor.Handles.RadiusHandle(Quaternion.identity, gameObject.transform.position, optimize.lightFadeDistance);
        }

        [UnityEditor.InitializeOnLoadMethod]
        static void EditorStaticInitialize() {
            UnityEditor.SceneManagement.PrefabStage.prefabStageOpened -= OnPrefabOpened;
            UnityEditor.SceneManagement.PrefabStage.prefabStageOpened += OnPrefabOpened;
        }

        static void OnPrefabOpened(UnityEditor.SceneManagement.PrefabStage prefabStage) {
            UnityEngine.Pool.ListPool<LightController>.Get(out var lightControllers);
            var prefab = prefabStage.prefabContentsRoot;
            prefab.GetComponentsInChildren(true, lightControllers);
            var dontHaveLightController = lightControllers.Count == 0;
            if (dontHaveLightController) {
                UnityEngine.Pool.ListPool<LightController>.Release(lightControllers);
                return;
            }

            for (int i = 0; i < lightControllers.Count; i++) {
                var lightController = lightControllers[i];
                if (lightController.bakedIsStatic == false && lightController.bakedNativeIntensity.Equals(default)) {
                    continue;
                }

                var nearestPrefabInstanceRoot = UnityEditor.PrefabUtility.GetNearestPrefabInstanceRoot(lightController.gameObject);
                if (nearestPrefabInstanceRoot != null && nearestPrefabInstanceRoot != prefabStage.prefabContentsRoot) {
                    string assetPath = UnityEditor.AssetDatabase.GetAssetPath(nearestPrefabInstanceRoot);
                    if (string.IsNullOrEmpty(assetPath)) {
                        continue;
                    }
                    using var nestedPrefabScope = new UnityEditor.PrefabUtility.EditPrefabContentsScope(assetPath);
                    string lightControllerTransformPath = UnityEditor.AnimationUtility.CalculateTransformPath(lightController.transform, nearestPrefabInstanceRoot.transform);
                    Transform lightControllerTransform = nestedPrefabScope.prefabContentsRoot.transform.Find(lightControllerTransformPath);
                    if (lightControllerTransform == null) {
                        continue;
                    }

                    var nestedPrefabLightController = lightControllerTransform.GetComponent<LightController>();
                    if (nestedPrefabLightController == null) {
                        continue;
                    }

                    var serializedLightController = new UnityEditor.SerializedObject(lightController);
                    UnityEditor.SerializedProperty bakedIsStaticProp = serializedLightController.FindProperty("bakedIsStatic");
                    UnityEditor.SerializedProperty bakedNativeIntensityProp = serializedLightController.FindProperty("bakedNativeIntensity");
                    if (bakedIsStaticProp.prefabOverride || bakedNativeIntensityProp.prefabOverride) {
                        UnityEditor.PrefabUtility.RevertPropertyOverride(bakedIsStaticProp, UnityEditor.InteractionMode.AutomatedAction);
                        UnityEditor.PrefabUtility.RevertPropertyOverride(bakedNativeIntensityProp, UnityEditor.InteractionMode.AutomatedAction);
                        serializedLightController.ApplyModifiedProperties();
                        UnityEditor.EditorUtility.SetDirty(lightController);
                    }

                    nestedPrefabLightController.bakedIsStatic = false;
                    nestedPrefabLightController.bakedNativeIntensity = default;
                    UnityEditor.EditorUtility.SetDirty(nestedPrefabLightController);
                } else {
                    lightController.bakedIsStatic = false;
                    lightController.bakedNativeIntensity = default;
                    UnityEditor.EditorUtility.SetDirty(lightController);
                }
            }

            UnityEngine.Pool.ListPool<LightController>.Release(lightControllers);
        }
#endif

        [Serializable]
        public struct ColorToggleObject {
            public bool useColor;
            public Gradient colorGradient;
        }

        [Serializable]
        public struct IntensityToggleObject {
            public bool useIntensity;

            [LabelWidth(160)]
            public ModType intensityType;

            [LabelWidth(160)]
            public IntensityUnit intensityUnits;

            [ShowIf("intensityType", ModType.Static), LabelWidth(160)]
            public float intensityStaticMin;

            [ShowIf("intensityType", ModType.Static), LabelWidth(160)]
            public float intensityStaticMax;

            [ShowIf("intensityType", ModType.Dynamic), LabelWidth(160)]
            public float intensityDynamicMin;

            [ShowIf("intensityType", ModType.Dynamic), LabelWidth(160)]
            public float intensityDynamicMax;

            [ShowIf("intensityType", ModType.Curve), LabelWidth(160)]
            public AnimationCurve intensityCurve;
        }

        [Serializable]
        public struct FadeEffectsToggleObject {
            public bool useFadeEffects;
            
            public bool fadeInOnEnable;
            [ShowIf(nameof(fadeInOnEnable)), Indent] public float fadeInTime;
            
            public bool fadeOutOnVfxStop;
            [ShowIf(nameof(fadeOutOnVfxStop)), Indent] public float fadeOutTime;
        }

        [Serializable]
        public struct NativeIntensityData : IEquatable<NativeIntensityData> {
            public AnimationCurve intensityCurve;
            public float intensityMinOrBaseIntensity;
            public float intensityMax;

            public bool Equals(NativeIntensityData other) {
                return (((intensityCurve == null || intensityCurve.length == 0) && (other.intensityCurve == null || other.intensityCurve.length == 0)) ||
                        intensityCurve.Equals(other.intensityCurve)) &&
                       intensityMinOrBaseIntensity.Equals(other.intensityMinOrBaseIntensity) && intensityMax.Equals(other.intensityMax);
            }

            public override bool Equals(object obj) {
                return obj is NativeIntensityData other && Equals(other);
            }

            public override int GetHashCode() {
                return HashCode.Combine(intensityCurve, intensityMinOrBaseIntensity, intensityMax);
            }
        }

        [Serializable]
        public struct RangeToggleObject {
            public bool useRange;

            [LabelWidth(160)]
            public ModType rangeType;

            [ShowIf("rangeType", ModType.Static), LabelWidth(160)]
            public float rangeStaticMin;

            [ShowIf("rangeType", ModType.Static), LabelWidth(160)]
            public float rangeStaticMax;

            [ShowIf("rangeType", ModType.Dynamic), LabelWidth(160)]
            public float rangeDynamicMin;

            [ShowIf("rangeType", ModType.Dynamic), LabelWidth(160)]
            public float rangeDynamicMax;

            [ShowIf("rangeType", ModType.Curve), LabelWidth(160)] public AnimationCurve rangeCurve;
            [ShowIf("rangeType", ModType.Curve), LabelWidth(160)] public float rangeMultiplier;
        }

        [Serializable]
        public struct TimeAndNoiseObject {
            [FormerlySerializedAs("timeMultiplier"), LabelWidth(160)] [Tooltip("The bigger the value, the slower time will run")]
            public float timeInverseMultiplier;

            [LabelWidth(160)] public float timeOffset;
            [FormerlySerializedAs("timeLoop"), LabelWidth(160)] public bool useTimeLoop;
            [ShowIf(nameof(useTimeLoop)), LabelWidth(160)] public float loopTime;
            [HideIf(nameof(useTimeLoop)), LabelWidth(160)] public float curvesTime;

            [Range(MinFrequency, MaxFrequency), LabelWidth(160)] public float noiseFrequency;

            [Space(10)]
            [LabelWidth(160)] public SeedOption seedOption;

            [ShowIf(nameof(seedOption), SeedOption.Shared), Required] public SeedSO noiseSeedSO;
            [ShowIf(nameof(seedOption), SeedOption.Inherit), Required] public LightController lightToCopySeedFrom;
            [ShowIf(nameof(seedOption), SeedOption.Specific), Required] public uint seed;

            public static TimeAndNoiseObject Default => new() {
                timeInverseMultiplier = 1, useTimeLoop = false, loopTime = 2, curvesTime = 2, noiseFrequency = 2,
                seedOption = SeedOption.Unique, seed = 995400
            };

#if UNITY_EDITOR
            [Button, ShowIf(nameof(seedOption), SeedOption.Shared)]
            void CreateNewNoiseSeedAsset() {
                var seedSO = ScriptableObject.CreateInstance<SeedSO>();
                seedSO.seed = new Unity.Mathematics.Random(GenerateUniqueSeedBasedOnCurrentTime()).state;
                var assetPath = UnityEditor.AssetDatabase.GenerateUniqueAssetPath("Assets/Data/Seeds/Lights/Seed.asset");
                UnityEditor.AssetDatabase.CreateAsset(seedSO, assetPath);
                UnityEditor.AssetDatabase.SaveAssets();
                UnityEditor.AssetDatabase.Refresh();
                noiseSeedSO = UnityEditor.AssetDatabase.LoadAssetAtPath<SeedSO>(assetPath);
            }
#endif
        }

        public enum SeedOption : byte {
            Unique = 0,
            Shared = 1,
            Inherit = 2,
            Specific = 3
        }

        [Serializable]
        public struct DayNightCycleToggleObject {
            public bool useDayNightCycle;
            [HorizontalGroup] public float baseIntensity;
            [HorizontalGroup, HideLabel] public UnityEngine.Rendering.LightUnit unit;
            public AnimationCurve intensityByDayTime;
#if UNITY_EDITOR
            [NonSerialized, ShowInInspector, Range(0, 1)] public float testDayTimeValue;
#endif
        }

        [Serializable]
        public struct OptimizeToggleObject {
            public bool useOptimize;
            public float shadowFadeDistance;
            public float lightFadeDistance;
        }

        public enum ModType : byte {
            Static,
            Dynamic,
            Curve
        }

        public enum IntensityUnit : byte {
            Lumen,
            Candela,
            Lux,
            Ev100
        }

        public enum LightSize : byte {
            [UnityEngine.Scripting.Preserve] Small = 0,
            [UnityEngine.Scripting.Preserve] Medium = 1,
            [UnityEngine.Scripting.Preserve] Big = 2,
            [UnityEngine.Scripting.Preserve] AlwaysVisible = 3
        }

        public enum UpdateType : byte {
            None = 0,
            ActiveUpdate = 1,
            CulledUpdate = 2,
            CulledButStaticSoNoUpdate = 3,
        }

#if UNITY_EDITOR
        public struct EditorAccess {
            public LightController lightController;
            public ref bool isStatic => ref lightController.bakedIsStatic;
            public bool forceStaticIfInitiallyOnScene => lightController.forceStaticIfInitiallyOnScene;
            public ref NativeIntensityData nativeIntensity => ref lightController.bakedNativeIntensity;

            public EditorAccess(LightController lightController) {
                this.lightController = lightController;
            }
        }
#endif
    }
}