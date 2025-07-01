using System;
using Awaken.TG.Code.Utility;
using Awaken.TG.Main.General.Configs;
using Awaken.TG.Main.Timing;
using Awaken.TG.MVC;
using Awaken.TG.MVC.Elements;
using Awaken.TG.Utility.Attributes;
using Awaken.Utility;
using Awaken.Utility.Maths.Data;
using Awaken.Utility.Times;
using UnityEngine;
using Random = UnityEngine.Random;

namespace Awaken.TG.Graphics {
    public partial class WeatherController : Element<GameRealTime> {
        public override ushort TypeForSerialization => SavedModels.WeatherController;

        const float VariationStabilizationTime = 30f;
        PrecipitationPreset[] _presets;
        
        float _heavyRainThreshold;

        [Saved] float _precipitationIntensity;
        [Saved] int _currentIndex = -1;
        [Saved] int _nextIndex = -1;
        [Saved] float _currentVariation;
        [Saved] float _targetVariation;
        [Saved] float _translation;
        [Saved] float _nextTranslation;
        [Saved] ARDateTime _lastTime;
        [Saved] DelayedValue _rainBlendIn;
        [Saved] DelayedValue _snowBlendIn;
        [Saved] bool _manuallyPrecipitationDisabled;
        [Saved] float _manualPrecipitationDisabledProgress;

        bool _heavyRain;
        public bool HeavyRain {
            get => _heavyRain;
            private set {
                if (value != _heavyRain) {
                    _heavyRain = value;
                    HeavyRainStateChanged?.Invoke();
                }
            }
        }

        public float PrecipitationIntensity {
            get => _precipitationIntensity;
            private set => _precipitationIntensity = value;
        }
        
        public float RainIntensity => PrecipitationIntensity * _rainBlendIn.Value;
        public float SnowIntensity => PrecipitationIntensity * _snowBlendIn.Value;
        public bool ManuallyPrecipitationDisabled => _manuallyPrecipitationDisabled;

        public ref DelayedValue RainBlendIn => ref _rainBlendIn;
        public ref DelayedValue SnowBlendIn => ref _snowBlendIn;

        public event Action HeavyRainStateChanged;
        
        protected override void OnInitialize() {
            Init();
            _lastTime = ParentModel.WeatherTime;
            _rainBlendIn.SetInstant(1);
            _snowBlendIn.SetInstant(0);
        }

        protected override void OnRestore() {
            Init();
        }

        protected override void OnFullyInitialized() {
            ParentModel.ListenTo(GameRealTime.Events.GameTimeChanged, OnTimeChanged, this);
        }

        void Init() {
            var gameConstants = World.Services.Get<GameConstants>();
            _presets = gameConstants.WeatherPresets;
            _heavyRainThreshold = gameConstants.HeavyRainThreshold;
        }

        public void ResumePrecipitation(bool instant) {
            _manuallyPrecipitationDisabled = false;
            if (instant) {
                _manualPrecipitationDisabledProgress = 0;
            }
        }

        public void StopPrecipitation(bool instant) {
            _manuallyPrecipitationDisabled = true;
            if (instant) {
                _manualPrecipitationDisabledProgress = 1;
            }
        }

        public void ManualOverrideToNextPreset() {
            SetCurrentPreset((_currentIndex + 1) % _presets.Length);
        }

        public void SetPreset(int preset) {
            if (preset >= 0 && preset < _presets.Length) {
                SetCurrentPreset(preset);
            }
        }
        
        void OnTimeChanged(ARDateTime time) {
            if (_currentIndex < 0 || _currentIndex >= _presets.Length) {
                SetCurrentPreset(RandomPresetIndex());
                SetNextPreset(RandomPresetIndex());
            }
            if (_lastTime.Day != time.Day) {
                SetCurrentPreset(_nextIndex, _nextTranslation);
                SetNextPreset(RandomPresetIndex());
            }
            
            PrecipitationPreset currentPreset = _presets[_currentIndex];
            var dayTime = time.DayTime;
            float translatedDayTime = (dayTime + _translation) % 1f;
            PrecipitationIntensity = currentPreset.intensityCurve.Evaluate(translatedDayTime);

            if (currentPreset.allowVariationOutsideOfRain | PrecipitationIntensity > 0) {
                ProcessVariation(time);
            }

            if (dayTime >= 0.9) {
                var t = (dayTime - 0.9f) / 0.1f;
                float startOfNextDay = _nextTranslation;
                PrecipitationIntensity = Mathf.Lerp(PrecipitationIntensity, _presets[_nextIndex].intensityCurve.Evaluate(startOfNextDay), t);
            }
            ProcessManualOverride();

            _lastTime = time;
            HeavyRain = PrecipitationIntensity >= _heavyRainThreshold;
        }

        void SetCurrentPreset(int preset, float? translation = null) {
            _currentIndex = preset;
            _translation = translation ?? Random.Range(0, _presets[_currentIndex].maxTranslation);
        }

        void SetNextPreset(int nextPreset) {
            _nextIndex = nextPreset;
            _nextTranslation = Random.Range(0, _presets[_nextIndex].maxTranslation);
        }

        void ProcessVariation(ARDateTime time) {
            var delta = Mathf.Abs(time.Minutes - _lastTime.Minutes) / VariationStabilizationTime;
            if (_lastTime.Hour != time.Hour) {
                var maxVariation = _presets[_currentIndex].maxVariation;
                _targetVariation = Random.Range(-maxVariation, maxVariation);
                delta = time.Minutes / VariationStabilizationTime;
            }
            _currentVariation = Mathf.MoveTowards(_currentVariation, _targetVariation, delta);
            PrecipitationIntensity += _currentVariation;
        }

        void ProcessManualOverride() {
            _manualPrecipitationDisabledProgress = Mathf.MoveTowards(_manualPrecipitationDisabledProgress, _manuallyPrecipitationDisabled ? 1 : 0, Time.deltaTime * 0.1f);
            PrecipitationIntensity = Mathf.Lerp(PrecipitationIntensity, 0, _manualPrecipitationDisabledProgress);
            PrecipitationIntensity = Mathf.Clamp01(PrecipitationIntensity);
        }

        int RandomPresetIndex() {
            return RandomUtil.WeightedSelect(0, _presets.Length-1, i => _presets[i].weight);
        }

        protected override void OnDiscard(bool fromDomainDrop) {
            HeavyRainStateChanged = null;
            base.OnDiscard(fromDomainDrop);
        }

        [Serializable]
        public struct PrecipitationPreset {
            [SerializeField] string name;
            public AnimationCurve intensityCurve;
            [Tooltip("Variation means that rain intensity will by randomized by value from -maxVariation to maxVariation")]
            [Range(0, 0.25f)] public float maxVariation;
            [Tooltip("If true, variation will be applied always. Otherwise variation will work only if curve is above 0")]
            public bool allowVariationOutsideOfRain;
            [Tooltip("Translation means that middle day rain might change to middle night rain etc. (time is translated)")]
            [Range(0, 1f)] public float maxTranslation;
            [Min(1)] public int weight;
        }
    }
}
