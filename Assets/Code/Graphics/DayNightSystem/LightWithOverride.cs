using System;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.Rendering.HighDefinition;
using Log = Awaken.Utility.Debugging.Log;

namespace Awaken.TG.Graphics.VFX {
    [RequireComponent(typeof(Light)), RequireComponent(typeof(HDAdditionalLightData)), DisallowMultipleComponent]
    public class LightWithOverride : MonoBehaviour {
        [SerializeField, Required, ReadOnly] new Light light;
        [SerializeField, Required, ReadOnly] HDAdditionalLightData lightData;

        int _colorOverrideCounter, _colorTemperatureOverrideCounter, _intensityOverrideCounter; 
        ValueWithOverrideWrapper<Color, Light> _colorValue;
        ValueWithOverrideWrapper<float, Light> _colorTemperatureValue;
        ValueWithOverrideWrapper<float, Light> _intensityValue;

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

        public float volumetricDimmer {
            get => lightData.volumetricDimmer;
            set => lightData.volumetricDimmer = value;
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

        [System.Diagnostics.Conditional("UNITY_EDITOR")]
        void EnsureInitializedInEditorMode() {
#if UNITY_EDITOR
            if (Application.isPlaying == false) {
                EnsureInitialized();
            } else if (IsInitialized == false) {
                throw new Exception($"Trying to use {nameof(LightWithOverride)} before it initialized");
            }
#endif
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
        }

        public void SetColorOverride(Color color, byte priority) {
            if (priority >= _colorValue.CurrentOverridePriority) {
                _colorValue.CurrentOverridePriority = priority;
                _colorValue.ValueWithOverride = color;
            }
        }

        public void StartColorOverride() {
            _colorOverrideCounter++;
            _colorValue.DoOverrideValue = true;
        }

        public void StopColorOverride() {
            if (_colorOverrideCounter == 0) {
                Log.Important?.Error("Stopping override more time than starting. Start and stop count should match");
                return;
            }
            _colorOverrideCounter--;
            if (_colorOverrideCounter == 0) {
                _colorValue.DoOverrideValue = false;
            }
        }

        public void SetColorTemperatureOverride(float colorTemperature, byte priority) {
            if (priority >= _colorTemperatureValue.CurrentOverridePriority) {
                _colorTemperatureValue.CurrentOverridePriority = priority;
                _colorTemperatureValue.ValueWithOverride = colorTemperature;
            }
        }

        public void StartColorTemperatureOverride() {
            _colorTemperatureOverrideCounter++;
            _colorTemperatureValue.DoOverrideValue = true;
        }

        public void StopColorTemperatureOverride() {
            if (_colorTemperatureOverrideCounter == 0) {
                Log.Important?.Error("Stopping override more time than starting. Start and stop count should match");
                return;
            }
            _colorTemperatureOverrideCounter--;
            if (_colorTemperatureOverrideCounter == 0) {
                _colorTemperatureValue.DoOverrideValue = false;
            }
        }

        public void SetIntensityOverride(float intensity, byte priority) {
            if (priority >= _intensityValue.CurrentOverridePriority) {
                _intensityValue.CurrentOverridePriority = priority;
                _intensityValue.ValueWithOverride = intensity;
            }
        }
        
        public void StartIntensityOverride() {
            _intensityOverrideCounter++;
            _intensityValue.DoOverrideValue = true;
        }

        public void StopIntensityOverride() {
            if (_intensityOverrideCounter == 0) {
                Log.Important?.Error("Stopping override more time than starting. Start and stop count should match");
                return;
            }
            _intensityOverrideCounter--;
            if (_intensityOverrideCounter == 0) {
                _intensityValue.DoOverrideValue = false;
            }
        }
        
        static Color GetColor(Light l) => l.color;
        static void SetColor(Light l, Color v) => l.color = v;

        static float GetColorTemperature(Light l) => l.colorTemperature;
        static void SetColorTemperature(Light l, float v) => l.colorTemperature = v;

        static float GetIntensity(Light l) => l.intensity;
        static void SetIntensity(Light l, float v) => l.intensity = v;

#if UNITY_EDITOR
        void Reset() {
            light = GetComponent<Light>();
            lightData = GetComponent<HDAdditionalLightData>();
        }
#endif

        unsafe struct ValueWithOverrideWrapper<T, TValueSource> {
            TValueSource _valueSource;
            // Unsafe function pointers. Same as Func<TValueSource, T> _getter, Action<TValueSource, T> _setter;
            delegate*<TValueSource, T> _getterFunc;
            delegate*<TValueSource, T, void> _setterFunc;
            T _notOverridenValue;
            T _overridenValue;
            bool _doOverrideValue;

            public bool HasValueSource => _valueSource != null;
            public byte CurrentOverridePriority { get; set; }

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
                set {
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
                _overridenValue = _notOverridenValue = valueSource != null ? getterFunc(valueSource) : default;
                CurrentOverridePriority = 0;
                _doOverrideValue = false;
            }
        }
    }
}