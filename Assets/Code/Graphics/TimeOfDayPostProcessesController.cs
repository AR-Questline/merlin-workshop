using System;
using System.Collections.Generic;
using System.Diagnostics;
using Awaken.TG.Debugging.Cheats;
using Awaken.TG.Main.Heroes;
using Awaken.TG.Main.Settings.Controllers;
using Awaken.TG.Main.Timing;
using Awaken.TG.MVC;
using Awaken.Utility.Graphics;
using Sirenix.OdinInspector;
using Unity.Mathematics;
#if UNITY_EDITOR
using UnityEditor;
#endif
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.HighDefinition;

namespace Awaken.TG.Graphics {
    [ExecuteAlways]
    public class TimeOfDayPostProcessesController : StartDependentView<GameRealTime> {
        const int MinutesInDay = 1440;

        #region PUBLIC
        public static float dayNightCycle;
        #endregion
        
        #region SERIALIZED FIELDS
        [ShowInInspector, BoxGroup("Time"), ProgressBar(0, 1, Height = 18), InlineButton(nameof(Stop)), GUIColor(nameof(GetButtonColor))]
        float _editorTimeOfDay => timeHours / 24f + timeMinutes / (float)MinutesInDay;
        [SerializeField, BoxGroup("Time")]
        bool useEditorTimeOfDay;
        bool UseEditorTimeOfDay {
            get {
#if UNITY_EDITOR
                return useEditorTimeOfDay;
#else
                return false;
#endif
            }
        }
        
        [ShowInInspector, BoxGroup("Time"), ProgressBar(0, 1, Height = 16, R = 0.65f, G = 0.35f, B = 0.3f), LabelText("Controller weather time"), ShowIf("@UnityEngine.Application.isPlaying")]
        float TimeOfDay => GenericTarget != null && !UseEditorTimeOfDay ? Target.WeatherTime.DayTime : _editorTimeOfDay;
        [ShowInInspector, BoxGroup("Time"), ProgressBar(0, 1, Height = 16, R = 0.35f, G = 0.65f, B = 0.3f), LabelText("Game weather time"), ShowIf("@UnityEngine.Application.isPlaying")]
        float GameTimeOfDay => GenericTarget != null ? Target.WeatherTime.DayTime : 0;
        [SerializeField, BoxGroup("Time"), Range(0, 23), OnValueChanged(nameof(ChangedTimeHoursMinutes))]
        int timeHours;
        [SerializeField, BoxGroup("Time"), Range(0, 59), OnValueChanged(nameof(ChangedTimeHoursMinutes))]
        int timeMinutes;
        [ShowInInspector, BoxGroup("Time"), Range(0, MinutesInDay), OnValueChanged(nameof(ChangedSingleEditorTimeOfDay))]
        int _singleEditorTimeOfDay;

        [SerializeField, FoldoutGroup("Day&Night")]
        AnimationCurve sunIntensity;
        [SerializeField, FoldoutGroup("Day&Night")]
        AnimationCurve sunIntensityMultiplier;
        [SerializeField, FoldoutGroup("Day&Night")]
        AnimationCurve sunTemperature;
        [SerializeField, FoldoutGroup("Day&Night")]
        AnimationCurve sunLensFlare;
        
        [Space]
        [SerializeField, FoldoutGroup("Day&Night")]
        AnimationCurve ambientIntensity;
        [SerializeField, FoldoutGroup("Day&Night")]
        AnimationCurve ambientTemperature = AnimationCurve.Linear(0, 6500, 1, 6500);
        
        [Space]
        [SerializeField, FoldoutGroup("Day&Night")]
        AnimationCurve rimIntensity;
        [SerializeField, FoldoutGroup("Day&Night")]
        AnimationCurve rimTemperature = AnimationCurve.Linear(0, 6500, 1, 6500);
        
        [Space]
        [SerializeField, FoldoutGroup("Day&Night")]
        AnimationCurve volumetricMultiplier = AnimationCurve.Linear(0, 16, 1, 16);
        
        // [Space]
        // [SerializeField, FoldoutGroup("Day&Night")]
        // AnimationCurve fogAttenuationDistance = AnimationCurve.Linear(0, 128, 1, 128);
        [SerializeField, FoldoutGroup("Day&Night")]
        AnimationCurve fogMaxHeight = AnimationCurve.Linear(0, 200, 1, 200);

        [SerializeField, FoldoutGroup("Day&Night"), FoldoutGroup("Day&Night/Duration")]
        Duration _dayDuration = new() {
            startTime = new() { hour = 6, minute = 0 },
            endTime = new() { hour = 19, minute = 0 },
        };
        
        [SerializeField, FoldoutGroup("Day&Night")]
        Vector3 _sunAxisRotation = Vector3.zero;
        [SerializeField, FoldoutGroup("Day&Night")]
        Vector3 _moonAxisRotation = Vector3.zero;
        [SerializeField, FoldoutGroup("Day&Night")]
        Vector2 _lightRotationSpace = new(95, -95);

        [SerializeField, FoldoutGroup("Volumes"), Required]
        Volume _currentCycleVolume;
        [SerializeField, FoldoutGroup("Volumes"), Required]
        Volume _nextCycleVolume;
        [SerializeField, FoldoutGroup("Volumes"), ValidateInput(nameof(ValidateVolumeDefinitions)), OnValueChanged(nameof(GenerateVolumeCurves), true)]
        VolumeDefinition[] _volumeDefinitions = Array.Empty<VolumeDefinition>();
        [SerializeField, FoldoutGroup("Volumes"), ListDrawerSettings(IsReadOnly = true)]
        NamedAnimationCurve[] _volumeBlendingCurves = Array.Empty<NamedAnimationCurve>();
        [ShowInInspector, FoldoutGroup("Volumes/DebugInfo"), MultiLineProperty]
        string VolumesDebugInfo => VolumesDebugInfoText();

        [SerializeField, FoldoutGroup("References"), Required] 
        Light sun;
        [SerializeField, FoldoutGroup("References"), Required] 
        Light ambientLight;
        [SerializeField, FoldoutGroup("References")] 
        Light rimLight;
        [SerializeField, FoldoutGroup("References"), Required] 
        Transform sunRotator;
        [SerializeField, FoldoutGroup("References")] [UnityEngine.Scripting.Preserve]
        LensFlareComponentSRP lensFlare;

        [SerializeField]
        bool showDebugGui;
        #endregion
        
        #region LOCAL
        HDAdditionalLightData _sunLightData;
        HDAdditionalLightData _ambientLightData;
        HDAdditionalLightData _rimLightData;
        bool _stop;
        int _timeOfDayStop;
        int _lastVolumeProfile = -1;
        List<IVolumeController> _volumeControllersBuffer = new(3);
        #endregion

        #region Properties
        Vector3? HeroPosition => Hero.Current?.Coords;
        #endregion
        
        #region MAIN
        void Start() {
            Initialization();
#if UNITY_EDITOR
            GetEditorPrefs();
#endif
            UpdateValues();
        }

        void Update() {
            dayNightCycle = TimeOfDay;

#if UNITY_EDITOR
            if (Application.isPlaying) {
                var position = HeroPosition;
                if (position.HasValue) {
                    SyncTimeParameters();
                }
            }
#endif

            UpdateValues();
            KeyboardControl();
            LensFlare();
        }

        void OnValidate() {
            _sunLightData = sun.GetComponent<HDAdditionalLightData>();
            _ambientLightData = ambientLight.GetComponent<HDAdditionalLightData>();
            
            if(rimLight != null)
                _rimLightData = rimLight.GetComponent<HDAdditionalLightData>();
            
            UpdateValues();
        }
        #endregion
        #region INITIALIZATION
        void Initialization() {
            _sunLightData = sun.GetComponent<HDAdditionalLightData>();
            _ambientLightData = ambientLight.GetComponent<HDAdditionalLightData>();
            lensFlare = sun.GetComponent<LensFlareComponentSRP>();
            _sunLightData.lightDimmer = 2.0f;
        }
        #endregion
        #region METHODS
        void UpdateValues() {
            LightData();
            LightPosition();
            Volumes();
        }

        void LightData() {
            if (_sunLightData == null || _ambientLightData == null) {
                return;
            }

            sun.intensity = sunIntensity.Evaluate(TimeOfDay);
            _sunLightData.lightDimmer = sunIntensityMultiplier.Evaluate(TimeOfDay);
            _sunLightData.volumetricDimmer = volumetricMultiplier.Evaluate(TimeOfDay);
            sun.colorTemperature = sunTemperature.Evaluate(TimeOfDay);

            ambientLight.intensity = ambientIntensity.Evaluate(TimeOfDay);
            ambientLight.colorTemperature = ambientTemperature.Evaluate(TimeOfDay);

            if (_rimLightData != null) {
                rimLight.intensity = rimIntensity.Evaluate(TimeOfDay);
                rimLight.colorTemperature = rimTemperature.Evaluate(TimeOfDay);
            }
        }

        void LightPosition() {
            sunRotator.localRotation = CalculateSunPosition(TimeOfDay);
        }

        void LensFlare() {
            // lensFlare.intensity = sunLensFlare.Evaluate(TimeOfDay);
            //
            // var heroPosition = HeroPosition;
            // if (heroPosition is { y: < 2.0f }) {
            //     lensFlare.intensity = 0.0f;
            // }
        }

        void Volumes() {
            if ((_volumeDefinitions?.Length ?? 0) == 0) {
                return;
            }
            
            var currentIndex = FindCurrentVolume(_volumeDefinitions, TimeOfDay);

            if (currentIndex == -1) {
                return;
            }
            
            var nextIndex = (currentIndex+1)%_volumeDefinitions.Length;
            var current = _volumeDefinitions[currentIndex];
            var next = _volumeDefinitions[nextIndex];

            if (current.profile == null || next.profile == null) {
                return;
            }

            if (_lastVolumeProfile != currentIndex) {
                _currentCycleVolume.SetSharedOrInstancedProfile(current.profile);
                _nextCycleVolume.SetSharedOrInstancedProfile(next.profile);

                RefreshVolumeControllers();
            }
            _lastVolumeProfile = currentIndex;

            var toNextBlend = 0f;
            if (current.duration.Overlap(next.duration, out var overlapDuration) && overlapDuration.IsIn(TimeOfDay)) {
                toNextBlend = overlapDuration.Progress(TimeOfDay);
                if (currentIndex < _volumeBlendingCurves.Length) {
                    toNextBlend = _volumeBlendingCurves[currentIndex].animationCurve.Evaluate(toNextBlend);
                }
            }
            _currentCycleVolume.weight = 1-toNextBlend;
            _nextCycleVolume.weight = 1;

            BlendFogProperties();
            UpdateFogValues(current, next);
        }
        
        void RefreshVolumeControllers() {
            _volumeControllersBuffer.Clear();
            _currentCycleVolume.GetComponents(_volumeControllersBuffer);
            foreach (var controller in _volumeControllersBuffer) {
                controller.NewVolumeProfileLoaded();
            }
            _volumeControllersBuffer.Clear();
            _nextCycleVolume.GetComponents(_volumeControllersBuffer);
            foreach (var controller in _volumeControllersBuffer) {
                controller.NewVolumeProfileLoaded();
            }
            _volumeControllersBuffer.Clear();
        }

        void BlendFogProperties() {
            foreach (var volumeDefinition in _volumeDefinitions) {
                VolumeProfile profile = volumeDefinition.profile;
                if (profile.TryGet<Fog>(out var fog)){
                    // fog.meanFreePath.value = fogAttenuationDistance.Evaluate(TimeOfDay);
                    fog.maximumHeight.value = fogMaxHeight.Evaluate(TimeOfDay);

                    fog.enabled.overrideState = true;
                }
            }
        }

        void UpdateFogValues(VolumeDefinition current, VolumeDefinition next) {
            var heroPositionNullable = HeroPosition;
            if (!heroPositionNullable.HasValue) {
                return;
            }
            var heroPosition = heroPositionNullable.Value;
            
            if (current.hasFogBaseHeightOverride && _currentCycleVolume.TryGetVolumeComponent<Fog>(out var fog)) {
                fog.baseHeight.value = current.fogBaseHeightByHeroYPosition.Evaluate(heroPosition.y);
            }
            if (next.hasFogBaseHeightOverride && _nextCycleVolume.TryGetVolumeComponent(out fog)) {
                fog.baseHeight.value = next.fogBaseHeightByHeroYPosition.Evaluate(heroPosition.y);
            }
        }

        static int FindCurrentVolume(VolumeDefinition[] definitions, float timeOfDay) {
            var firstIndex = Array.FindIndex(definitions, vd => vd.duration.IsIn(timeOfDay));
            if (firstIndex == definitions.Length-1) {
                return firstIndex;
            }
            var secondIndex = Array.FindIndex(definitions, firstIndex+1, vd => vd.duration.IsIn(timeOfDay));
            if (secondIndex == -1) {
                return firstIndex;
            }
            var first = definitions[firstIndex];
            var second = definitions[secondIndex];
            return math.abs(first.duration.endTime.Normalized-timeOfDay) <
                   math.abs(second.duration.endTime.Normalized-timeOfDay) ?
                firstIndex :
                secondIndex;
        }

#if UNITY_EDITOR
        void GetEditorPrefs() {
            if (!Application.isPlaying) {
                _timeOfDayStop = EditorPrefs.GetInt("TimeOfDayStop");
                if (_timeOfDayStop == 1) {
                    _stop = true;
                    timeHours = EditorPrefs.GetInt("TimeOfDayHours");
                    timeMinutes = EditorPrefs.GetInt("TimeOfDayMinutes");
                } else {
                    _stop = false;
                }
            } else {
                _stop = false;
            }
        }

        void SetEditorPrefs() {
            if (!Application.isPlaying && _stop) {
                EditorPrefs.SetInt("TimeOfDayStop", 0);
            } else {
                EditorPrefs.SetInt("TimeOfDayStop", 1);   
                EditorPrefs.SetInt("TimeOfDayHours", timeHours);   
                EditorPrefs.SetInt("TimeOfDayMinutes", timeMinutes);  
            }
        }

        VolumeStack _stack;
        void OnGUI() {
            if (!showDebugGui || !Application.isPlaying) {
                return;
            }
            
            _stack ??= VolumeManager.instance.CreateStack();
            var mainCamera = Camera.main;
            mainCamera.TryGetComponent<HDAdditionalCameraData>(out var mainCamAdditionalData);
            VolumeManager.instance.Update(_stack, mainCamera.transform, mainCamAdditionalData.volumeLayerMask);
            var exposure = _stack.GetComponent<Exposure>();
            // var hdri = _stack.GetComponent<HDRISky>();

            var labelStyle = new GUIStyle(EditorStyles.label) {
                fontSize = 20,
            };

            GUILayout.BeginHorizontal();
            GUILayout.Space(24);
            GUILayout.BeginVertical();
            GUILayout.Space(24);

            GUI.contentColor = new Color(.88f, .88f, 0.88f);

            GUILayout.Label($"TIME: {timeHours}:{timeMinutes}", labelStyle);
            GUILayout.Label($"TIME N: {TimeOfDay}", labelStyle);
            GUILayout.Label($"SUN INTENSITY: {sunIntensity.Evaluate(TimeOfDay)}", labelStyle);
            GUILayout.Label($"AMBIENT INTENSITY: {ambientIntensity.Evaluate(TimeOfDay)}", labelStyle);
            GUILayout.Label($"EXPOSURE: {exposure.fixedExposure.value}", labelStyle);
            // GUILayout.Label($"HDRI EXPOSURE: {hdri.exposure.value}", labelStyle);
            
            GUILayout.EndVertical();
            GUILayout.EndHorizontal();
        }
#endif
        void Stop() {
#if UNITY_EDITOR
            SetEditorPrefs();
#endif
            _stop = !_stop;
        }
        
        Color GetButtonColor() {
            return this._stop ? Color.red : Color.white;
        }

        void KeyboardControl() {
            if (!CheatController.CheatsEnabled()) return;
            if (Input.GetKeyDown(KeyCode.KeypadMultiply)) {
                Stop();
            }
            if (Input.GetKeyDown(KeyCode.KeypadPlus) || Input.GetKeyDown(KeyCode.Equals)) {
                Target.WeatherIncrementDayFloat(0.02f);
            }
            if (Input.GetKeyDown(KeyCode.KeypadMinus) || Input.GetKeyDown(KeyCode.Minus)) {
                if (TimeOfDay < 0.1) {
                    Target.WeatherIncrementDayFloat(1f - TimeOfDay);
                }
                Target.WeatherIncrementDayFloat(-0.02f);
            }
        }

        void ChangedSingleEditorTimeOfDay() {
            timeHours = _singleEditorTimeOfDay/60%24;
            timeMinutes = _singleEditorTimeOfDay%60;
        }
        void ChangedTimeHoursMinutes() {
            _singleEditorTimeOfDay = timeHours*60+timeMinutes;
        }
        
        [Conditional("UNITY_EDITOR")]
        void SyncTimeParameters() {
            if (UseEditorTimeOfDay && GenericTarget != null) {
                if (World.All<TimeBlocker>().FirstOrDefault(t => t.SourceID == Target.ID) == null) {
                    World.Add(new TimeBlocker(Target.ID, TimeType.Weather));
                }
                Target.SetWeatherTime(timeHours, timeMinutes);
                return;
            }
            if (GenericTarget != null) {
                World.All<TimeBlocker>().FirstOrDefault(t => t.SourceID == Target.ID)?.Discard();
            }
            _singleEditorTimeOfDay = (int)(MinutesInDay*TimeOfDay);
            ChangedSingleEditorTimeOfDay();
        }

        bool ValidateVolumeDefinitions(VolumeDefinition[] definitions, ref string message) {
            if (definitions.Length < 2) {
                message += "Please provide at least two volume definitions";
                return false;
            }

            for (int i = 0; i < definitions.Length; i++) {
                if (definitions[i].profile == null) {
                    message += $"Definition at {i} is missing volume profile";
                    return false;
                }
            }

            for (var i = 0; i < MinutesInDay; i++) {
                if (FindCurrentVolume(definitions, i/(float)MinutesInDay) == -1) {
                    var h = i/60%24;
                    var m = i%60;
                    message += $" There is hole for {h}:{m}.";
                    return false;
                }
            }
            
            return true;
        }

        void GenerateVolumeCurves() {
            var previousSize = _volumeBlendingCurves?.Length ?? 0;
            if (_volumeBlendingCurves != null) {
                Array.Resize(ref _volumeBlendingCurves, _volumeDefinitions.Length);
            } else {
                _volumeBlendingCurves = new NamedAnimationCurve[_volumeDefinitions.Length];
            }

            for (var i = previousSize; i < _volumeBlendingCurves.Length; i++) {
                _volumeBlendingCurves[i] = new() {
                    animationCurve = AnimationCurve.Linear(0, 0, 1, 1),
                };
            }
            
            for (var i = 0; i < _volumeDefinitions.Length; i++) {
                var from = _volumeDefinitions[i].profile?.name ?? "Null";
                var to = _volumeDefinitions[(i+1)%_volumeDefinitions.Length].profile?.name ?? "Null";
                _volumeBlendingCurves[i].name = $"{from} -> {to}";
            }
        }

        Quaternion CalculateSunPosition(float timeOfDay) {
            if (_dayDuration.IsIn(timeOfDay)) {
                var axisRotation = Quaternion.Euler(_sunAxisRotation);
                var zRotation = Mathf.Lerp(_lightRotationSpace.x, _lightRotationSpace.y, _dayDuration.Progress(timeOfDay));
                return axisRotation * Quaternion.Euler(0, 0, zRotation);
            } else {
                var axisRotation = Quaternion.Euler(_moonAxisRotation);
                var zRotation = Mathf.Lerp(_lightRotationSpace.y, _lightRotationSpace.x, _dayDuration.Inverse().Progress(timeOfDay));
                return axisRotation * Quaternion.Euler(0, 0, zRotation);
            }
        }
        
        string VolumesDebugInfoText() {
            return $@"Hero position: {HeroPosition}";
        }
        #endregion Methods

        [Serializable]
        class VolumeDefinition {
            public VolumeProfile profile;
            public Duration duration;

            [HorizontalGroup("HasFogOverride", 0.1f), LabelText("Fog base height override"), ValidateInput(nameof(CanOverrideFogBaseHeight))]
            public bool hasFogBaseHeightOverride;
            [HorizontalGroup("HasFogOverride"), ShowIf(nameof(hasFogBaseHeightOverride)), HideLabel]
            public AnimationCurve fogBaseHeightByHeroYPosition = AnimationCurve.Linear(-20, 0, 20, 1);
            
            bool CanOverrideFogBaseHeight(bool value, ref string message) {
                if (value) {
                    if (!profile.TryGet<Fog>(out var fog)) {
                        message += "Cannot override fog base height because profile dont have Fog";
                        return false;
                    }
                    if (!fog.baseHeight.overrideState) {
                        message += "Cannot override fog base height because fog don't have override for base height";
                        return false;
                    }
                }
                return true;
            }
        }
        
        [Serializable, InlineProperty, HideLabel]
        struct Duration {
            public TimeDefinition startTime;
            public TimeDefinition endTime;

            public bool IsIn(float normalizedTime) {
                if (startTime.Normalized < endTime.Normalized) {
                    return startTime.Normalized <= normalizedTime && normalizedTime < endTime.Normalized;
                }
                return startTime.Normalized <= normalizedTime || normalizedTime < endTime.Normalized;
            }

            public float Progress(float normalizedTime) {
                if (startTime.Normalized < endTime.Normalized) {
                    var range = endTime.Normalized-startTime.Normalized;
                    return (normalizedTime-startTime.Normalized)/range;
                } else {
                    var range = (1+endTime.Normalized)-startTime.Normalized;
                    normalizedTime = normalizedTime < startTime.Normalized ? normalizedTime+1 : normalizedTime;
                    return (normalizedTime-startTime.Normalized)/range;
                }
            }
            
            public bool Overlap(Duration other, out Duration output) {
                if (IsIn(other.startTime.Normalized)) {
                    var start = other.startTime;
                    var end = IsIn(other.endTime.Normalized) ? other.endTime : endTime;
                    output = new() { startTime = start, endTime = end };
                    return true;
                }
                if (other.IsIn(startTime.Normalized)) {
                    var start = startTime;
                    var end = IsIn(other.endTime.Normalized) ? other.endTime : endTime;
                    output = new() { startTime = start, endTime = end };
                    return true;
                }
                output = default;
                return false;
            }
            
            public Duration Inverse() {
                return new() {
                    startTime = endTime,
                    endTime = startTime,
                };
            }
        }

        [Serializable]
        struct TimeDefinition : IComparable<TimeDefinition>, IEquatable<TimeDefinition> {
            [OnValueChanged(nameof(CalculateNormalized)), Range(0, 23)] public int hour;
            [OnValueChanged(nameof(CalculateNormalized)), Range(0, 59)] public int minute;
            [SerializeField, ReadOnly] float _normalized;

            public float Normalized => _normalized;

            void CalculateNormalized() {
                _normalized = hour / 24f + minute / (float)MinutesInDay;
            }

            #region Equality
            public int CompareTo(TimeDefinition other) {
                return _normalized.CompareTo(other._normalized);
            }

            public bool Equals(TimeDefinition other) {
                return _normalized.Equals(other._normalized);
            }
            public override bool Equals(object obj) {
                return obj is TimeDefinition other && Equals(other);
            }
            public override int GetHashCode() {
                return _normalized.GetHashCode();
            }
            public static bool operator ==(TimeDefinition left, TimeDefinition right) {
                return left.Equals(right);
            }
            public static bool operator !=(TimeDefinition left, TimeDefinition right) {
                return !left.Equals(right);
            }
            public static bool operator <(TimeDefinition left, TimeDefinition right) {
                return left._normalized < right._normalized;
            }
            public static bool operator >(TimeDefinition left, TimeDefinition right) {
                return left._normalized > right._normalized;
            }
            public static bool operator <=(TimeDefinition left, TimeDefinition right) {
                return left._normalized <= right._normalized;
            }
            public static bool operator >=(TimeDefinition left, TimeDefinition right) {
                return left._normalized >= right._normalized;
            }
            #endregion Equality
        }

        /// <summary>
        /// AnimationCurve but with some Odin styling for better UX in current context
        /// </summary>
        [Serializable, InlineProperty, HideLabel]
        class NamedAnimationCurve {
            [HideInInspector] public string name;
            [BoxGroup("$name"), HideLabel] public AnimationCurve animationCurve;
        }
    }
}
