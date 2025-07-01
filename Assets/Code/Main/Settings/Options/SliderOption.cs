using System;
using Awaken.TG.Main.Memories;
using Awaken.TG.Main.Settings.Options.Views;
using UnityEngine;

namespace Awaken.TG.Main.Settings.Options {
    public class SliderOption : PrefOption {
        const float NaviChange = 0.1f;

        public Action<float> onChange;
        
        float _value;
        float _inGameValue;
        float _defaultValue;
        string _valueFormat;
        
        public override Type ViewType => typeof(VSlider);
        public override bool WasChanged => _inGameValue != _value;

        public float MinValue { get; }
        public float MaxValue { get; }
        public float ChangePerStep { get; }
        public bool WholeNumbers { get; }

        public float Value {
            get => _value;
            set {
                var newValue = Mathf.Clamp(value, MinValue, MaxValue);
                if (Mathf.Approximately(_value, newValue)) {
                    return;
                }
                _value = newValue;
                PrefMemory.Set(PrefKey, _value, Synchronize);
                onChange?.Invoke(_value);
            }
        }

        public string DisplayValue => string.Format(_valueFormat, Value);

        public SliderOption(string settingId, string displayName, float minValue, float maxValue, bool wholeNumbers, string valueFormat, float defaultValue, bool synchronize, float? stepChange = null) : base(settingId, displayName, synchronize) {
            MinValue = minValue;
            MaxValue = maxValue;
            ChangePerStep = stepChange ?? ((MaxValue - MinValue) * NaviChange);
            WholeNumbers = wholeNumbers;
            var savedValue = PrefMemory.GetFloat(PrefKey, defaultValue);
            _value = Mathf.Clamp(savedValue, MinValue, MaxValue);
            _inGameValue = _value;
            _defaultValue = defaultValue;
            _valueFormat = valueFormat;
        }

        public override void ForceChange() {
            onChange?.Invoke(_value);
        }

        public override void Apply() {
            _inGameValue = Value;
        }

        public override void Cancel() {
            Value = _inGameValue;
        }

        public override void RestoreDefault() {
            Value = _defaultValue;
        }
    }
}