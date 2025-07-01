using System;
using Awaken.TG.Main.Memories;
using Awaken.TG.Main.Settings.Options.Views;

namespace Awaken.TG.Main.Settings.Options {
    public class ToggleOption : PrefOption {
        public override Type ViewType => typeof(VToggle);
        public override bool WasChanged => _inGameValue != _value;

        public Action<bool> onChange;
        bool _value;
        bool _inGameValue;
        bool _defaultValue;
        
        public bool DefaultValue => _defaultValue;
        public bool Enabled {
            get => _value;
            set {
                _value = value;
                PrefMemory.Set(PrefKey, value, Synchronize);
                onChange?.Invoke(value);
            }
        }

        public ToggleOption(string settingId, string displayName, bool defaultValue, bool synchronize) : base(settingId, displayName, synchronize) {
            _value = PrefMemory.GetBool(PrefKey, defaultValue);
            _inGameValue = _value;
            _defaultValue = defaultValue;
        }

        public override void ForceChange() {
            onChange?.Invoke(_value);
        }

        public override void Apply() {
            _inGameValue = Enabled;
        }

        public override void Cancel() {
            Enabled = _inGameValue;
        }

        public override void RestoreDefault() {
            Enabled = _defaultValue;
        }
    }
}