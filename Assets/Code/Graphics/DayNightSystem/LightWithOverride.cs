using System;
using System.Collections.Generic;
using Awaken.Utility.Collections;
using Sirenix.OdinInspector;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using UnityEngine;
using UnityEngine.Rendering.HighDefinition;

namespace Awaken.TG.Graphics.VFX {
    [RequireComponent(typeof(Light)), RequireComponent(typeof(HDAdditionalLightData)), DisallowMultipleComponent]
    public class LightWithOverride : MonoBehaviour {
        [SerializeField, Required, Sirenix.OdinInspector.ReadOnly] new Light light;
        [SerializeField, Required, Sirenix.OdinInspector.ReadOnly] HDAdditionalLightData lightData;
        
        ValueWithOverrideWrapper<Color, Light> _colorValue;
        ValueWithOverrideWrapper<float, Light> _colorTemperatureValue;
        ValueWithOverrideWrapper<float, Light> _intensityValue;
        ValueWithOverrideWrapper<float, HDAdditionalLightData> _volumetricDimmerValue;

        public Light Light => light;
        public HDAdditionalLightData LightData => lightData;

        public Color color {
            get {
                EnsureInitializedInEditorMode();
                return _colorValue.Value;
            }
            set {
                EnsureInitializedInEditorMode();
                _colorValue.Value = value;
            }
        }

        public Color ColorWithOverride => _colorValue.ValueWithOverride;

        public bool OverrideColor => _colorValue.DoOverrideValue;

        public float colorTemperature {
            get {
                EnsureInitializedInEditorMode();
                return _colorTemperatureValue.Value;
            }
            set {
                EnsureInitializedInEditorMode();
                _colorTemperatureValue.Value = value;
            }
        }

        public float ColorTemperatureWithOverride => _colorTemperatureValue.ValueWithOverride;

        public bool OverrideColorTemperature => _colorTemperatureValue.DoOverrideValue;

        public float intensity {
            get {
                EnsureInitializedInEditorMode();
                return _intensityValue.Value;
            }
            set {
                EnsureInitializedInEditorMode();
                _intensityValue.Value = value;
            }
        }

        public float IntensityWithOverride => _intensityValue.ValueWithOverride;

        public bool OverrideIntensity => _intensityValue.DoOverrideValue;

        public float volumetricDimmer {
            get {
                EnsureInitializedInEditorMode();
                return _volumetricDimmerValue.Value;
            }
            set {
                EnsureInitializedInEditorMode();
                _volumetricDimmerValue.Value = value;
            }
        }

        public float VolumetricDimmerWithOverride => _volumetricDimmerValue.ValueWithOverride;
        
        public bool OverrideVolumetricDimmer => _volumetricDimmerValue.DoOverrideValue;
        
        public Texture cookie {
            get => light.cookie;
            set => light.cookie = value;
        }

        public LightShadows shadows {
            get => light.shadows;
            set => light.shadows = value;
        }

        public Color surfaceTint {
            get => lightData.surfaceTint;
            set => lightData.surfaceTint = value;
        }

        public float flareSize {
            get => lightData.flareSize;
            set => lightData.flareSize = value;
        }

        public float flareMultiplier {
            get => lightData.flareMultiplier;
            set => lightData.flareMultiplier = value;
        }

        public float lightDimmer {
            get => lightData.lightDimmer;
            set => lightData.lightDimmer = value;
        }
        
        public float shapeWidth {
            get => lightData.shapeWidth;
            set => lightData.shapeWidth = value;
        }

        public float shapeHeight {
            get => lightData.shapeHeight;
            set => lightData.shapeHeight = value;
        }

        public bool IsInitialized => _intensityValue.HasValueSource;

        void Awake() {
            if (light == null) {
                light = GetComponent<Light>();
            }
            if (lightData == null) {
                lightData = GetComponent<HDAdditionalLightData>();
            }
            EnsureInitialized();
        }

        void OnDestroy() {
            _colorValue.Dispose();
            _colorTemperatureValue.Dispose();
            _intensityValue.Dispose();
            _volumetricDimmerValue.Dispose();
        }

        unsafe void EnsureInitialized() {
            if (light == null) {
                throw new NullReferenceException($"Light in {nameof(LightWithOverride)} is not assigned");
            }
            if (IsInitialized) {
                return;
            }
            _colorValue = new ValueWithOverrideWrapper<Color, Light>(light, &GetColor, &SetColor);
            _colorTemperatureValue = new ValueWithOverrideWrapper<float, Light>(light, &GetColorTemperature, &SetColorTemperature);
            _intensityValue = new ValueWithOverrideWrapper<float, Light>(light, &GetIntensity, &SetIntensity);
            _volumetricDimmerValue = new ValueWithOverrideWrapper<float, HDAdditionalLightData>(lightData, &GetVolumetricDimmer, &SetVolumetricDimmer);
        }
        
        [System.Diagnostics.Conditional("UNITY_EDITOR")]
        public void EnsureInitializedInEditorMode() {
#if UNITY_EDITOR
            if (Application.isPlaying == false) {
                EnsureInitialized();
            } else if (IsInitialized == false) {
                throw new Exception($"Trying to use {nameof(LightWithOverride)} before it initialized");
            }
#endif
        }

        public void ClearAllOverrides() {
            _colorValue.ClearOverrides();
            _colorTemperatureValue.ClearOverrides();
            _intensityValue.ClearOverrides();
            _volumetricDimmerValue.ClearOverrides();
        }
        public void SetColorOverride(Color color, byte priority) {
            _colorValue.SetOverrideValue(color, priority);
        }

        public void StartColorOverride(byte priority) {
            _colorValue.StartOverride(priority);
        }

        public void StopColorOverride(byte priority) {
            _colorValue.StopOverride(priority);
        }
        
        public Color GetColorWithLowerPriority(byte priority) {
            return _colorValue.GetValueWithLowerPriority(priority);
        }

        public void SetColorTemperatureOverride(float colorTemperature, byte priority) {
            _colorTemperatureValue.SetOverrideValue(colorTemperature, priority);
        }

        public void StartColorTemperatureOverride(byte priority) {
            _colorTemperatureValue.StartOverride(priority);
        }

        public void StopColorTemperatureOverride(byte priority) {
            _colorTemperatureValue.StopOverride(priority);
        }
        
        public float GetColorTemperatureWithLowerPriority(byte priority) {
            return _colorTemperatureValue.GetValueWithLowerPriority(priority);
        }

        public void SetIntensityOverride(float intensity, byte priority) {
            _intensityValue.SetOverrideValue(intensity, priority);
        }
        
        public void StartIntensityOverride(byte priority) {
            _intensityValue.StartOverride(priority);
        }

        public void StopIntensityOverride(byte priority) {
            _intensityValue.StopOverride(priority);
        }
        
        public float GetIntensityWithLowerPriority(byte priority) {
            return _intensityValue.GetValueWithLowerPriority(priority);
        }
        
        public void SetVolumetricDimmerOverride(float volumetricDimmer, byte priority) {
            _volumetricDimmerValue.SetOverrideValue(volumetricDimmer, priority);
        }
        
        public void StartVolumetricDimmerOverride(byte priority) {
            _volumetricDimmerValue.StartOverride(priority);
        }

        public void StopVolumetricDimmerOverride(byte priority) {
            _volumetricDimmerValue.StopOverride(priority);
        }
        
        public float GetVolumetricDimmerWithLowerPriority(byte priority) {
            return _volumetricDimmerValue.GetValueWithLowerPriority(priority);
        }
        
        static Color GetColor(Light l) => l.color;
        static void SetColor(Light l, Color v) => l.color = v;

        static float GetColorTemperature(Light l) => l.colorTemperature;
        static void SetColorTemperature(Light l, float v) => l.colorTemperature = v;

        static float GetIntensity(Light l) => l.intensity;
        static void SetIntensity(Light l, float v) => l.intensity = v;

        static float GetVolumetricDimmer(HDAdditionalLightData hdLightData) => hdLightData.volumetricDimmer;
        static void SetVolumetricDimmer(HDAdditionalLightData hdLightData, float v) => hdLightData.volumetricDimmer = v;

#if UNITY_EDITOR
        void Reset() {
            light = GetComponent<Light>();
            lightData = GetComponent<HDAdditionalLightData>();
        }
#endif

        public unsafe struct ValueWithOverrideWrapper<T, TValueSource> : IDisposable where T : unmanaged {
            TValueSource _valueSource;
            // Unsafe function pointers. Same as Func<TValueSource, T> _getter, Action<TValueSource, T> _setter;
            delegate*<TValueSource, T> _getterFunc;
            delegate*<TValueSource, T, void> _setterFunc;
            UnsafeList<ValueWithPriorityData> _overrideValues;
            T _notOverridenValue;
            T _overridenValue;
            bool _doOverrideValue;
            public bool HasValueSource => _valueSource != null;

            public void StartOverride(byte priority) {
                bool isNewPriority = true;
                for (int i = 0; i < _overrideValues.Length; i++) {
                    ref var overrideValue = ref _overrideValues.Ptr[i];
                    if (overrideValue.priority == priority) {
                        overrideValue.value = _notOverridenValue;
                        overrideValue.thisPriorityOverridesCount++;
                        isNewPriority = false;
                        break;
                    }
                }
                if (isNewPriority) {
                    _overrideValues.Add(new(_notOverridenValue, priority, 1));
                    _overrideValues.Sort(new PriorityComparer());
                }
                DoOverrideValue = true;
                ValueWithOverride = _overrideValues[^1].value;
            }

            public void StopOverride(byte priority) {
                for (int i = 0; i < _overrideValues.Length; i++) {
                    ref var overrideValue = ref _overrideValues.Ptr[i];
                    if (overrideValue.priority == priority) {
                        overrideValue.thisPriorityOverridesCount--;
                        if (overrideValue.thisPriorityOverridesCount <= 0) {
                            _overrideValues.RemoveAt(i);
                        }
                        break;
                    }
                }
                DoOverrideValue = _overrideValues.Length > 0;
                ValueWithOverride = DoOverrideValue ? _overrideValues[^1].value : _notOverridenValue;
            }

            public void SetOverrideValue(T value, byte priority) {
                for (int i = 0; i < _overrideValues.Length; i++) {
                    ref var overrideValue = ref _overrideValues.Ptr[i];
                    if (overrideValue.priority == priority) {
                        overrideValue.value = value;
                        if (i == _overrideValues.Length - 1) {
                            ValueWithOverride = value;
                        }
                        break;
                    }
                }
            }

            public void ClearOverrides() {
                _overrideValues.Clear();
                DoOverrideValue = false;
            }

            public T GetValueWithLowerPriority(byte priority) {
                for (int i = _overrideValues.Length - 1; i >= 0; i--) {
                    ref var overrideValue = ref _overrideValues.Ptr[i];
                    if (overrideValue.priority < priority) {
                        return overrideValue.value;
                    }
                }
                return _notOverridenValue;
            }
            
            public T Value {
                get => _doOverrideValue ? _notOverridenValue : _getterFunc(_valueSource);
                set {
                    _notOverridenValue = value;
                    if (_doOverrideValue == false) {
                        _setterFunc(_valueSource, value);
                    }
                }
            }

            public T ValueWithOverride {
                get => _doOverrideValue == false ? _overridenValue : _getterFunc(_valueSource);
                set {
                    _overridenValue = value;
                    if (_doOverrideValue) {
                        _setterFunc(_valueSource, value);
                    }
                }
            }

            public bool DoOverrideValue {
                get => _doOverrideValue;
                private set {
                    if (!_doOverrideValue && value) {
                        _notOverridenValue = _getterFunc(_valueSource);
                        _doOverrideValue = true;
                        _setterFunc(_valueSource, _overridenValue);
                    } else if (_doOverrideValue && !value) {
                        _setterFunc(_valueSource, _notOverridenValue);
                        _doOverrideValue = false;
                    }
                }
            }

            public ValueWithOverrideWrapper(TValueSource valueSource, delegate*<TValueSource, T> getterFunc, delegate*<TValueSource, T, void> setterFunc) {
                this._valueSource = valueSource;
                this._getterFunc = getterFunc;
                this._setterFunc = setterFunc;
                _overrideValues = new UnsafeList<ValueWithPriorityData>(4, ARAlloc.Domain);
                _overridenValue = _notOverridenValue = valueSource != null ? getterFunc(valueSource) : default;
                _doOverrideValue = false;
            }

            public void Dispose() {
                _overrideValues.Dispose();
            }

            struct ValueWithPriorityData {
                public T value;
                public byte priority;
                public byte thisPriorityOverridesCount;
                
                public ValueWithPriorityData(T value, byte priority, byte thisPriorityOverridesCount) {
                    this.value = value;
                    this.priority = priority;
                    this.thisPriorityOverridesCount = thisPriorityOverridesCount;
                }
            }
            struct PriorityComparer : IComparer<ValueWithPriorityData> {
                public int Compare(ValueWithPriorityData x, ValueWithPriorityData y) {
                    return x.priority.CompareTo(y.priority);
                }
            }
        }
    }
}