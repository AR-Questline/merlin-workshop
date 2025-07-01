using System;
using System.Collections.Generic;
using System.Linq;
using Awaken.TG.Main.Memories;
using Awaken.TG.Main.Settings.Options.Views;
using Awaken.Utility.Collections;

namespace Awaken.TG.Main.Settings.Options {
    public class EnumArrowsOption : PrefOption {
        public override Type ViewType => typeof(VEnumArrows);
        public override bool WasChanged => _option != _inGameValue;
        public Action<ToggleOption> onChange;
        public Action<ToggleOption> onForbiddenOptionsChange;
        public Action<ToggleOption> onChangeByPlayer;

        public IEnumerable<ToggleOption> Options => _options;

        ToggleOption[] _options;
        ToggleOption[] _forbiddenOptions;
        ToggleOption _option;
        ToggleOption _inGameValue;
        ToggleOption _defaultValue;

        public int OptionInt {
            get => Array.IndexOf(_options, _option);
            set => Option = _options[value];
        }

        public ToggleOption Option {
            get => _option;
            set {
                _option.Enabled = false;
                _option = value;
                _option.Enabled = true;
                PrefMemory.Set(PrefKey, OptionInt, Synchronize);
                onChange?.Invoke(_option);
            }
        }
        
        ToggleOption Next {
            get {
                for (int i = OptionInt + 1; i < _options.Length; i++) {
                    if (!IsForbidden(_options[i])) {
                        return _options[i];
                    }
                }
                return null;
            }
        }
        ToggleOption Previous {
            get {
                for (int i = OptionInt - 1; i >= 0; i--) {
                    if (!IsForbidden(_options[i])) {
                        return _options[i];
                    }
                }
                return null;
            }
        }

        public bool CanChooseNextOption => Next != null;
        public bool CanChoosePrevOption => Previous != null;
        
        public EnumArrowsOption(string settingId, string displayName, ToggleOption defaultValue, bool synchronize,
            params ToggleOption[] options) : base(settingId, displayName, synchronize) {
            _options = options;
            _defaultValue = defaultValue ?? options[0];
            int index = PrefMemory.GetInt(PrefKey, -1);
            if (index >= 0 && index < _options.Length) {
                _option = _options[index];
            } else {
                _option = _defaultValue;
            }
            _inGameValue = _option;
        }

        public void SetForbiddenOptions(params ToggleOption[] options) {
            _forbiddenOptions = options;
            onForbiddenOptionsChange?.Invoke(_option);
        }

        public override void ForceChange() {
            onChange?.Invoke(_option);
        }

        public override void Apply() {
            _inGameValue.Apply();
            Option.Apply();
            _inGameValue = Option;
        }

        public override void Cancel() {
            Option = _inGameValue;
        }

        public override void RestoreDefault() {
            Option = _defaultValue;
        }

        public void NextOption() {
            if (!CanChooseNextOption) {
                return;
            }

            Option = Next;
            onChangeByPlayer?.Invoke(_option);
        }

        public void NextOptionCarousel() {
            var currentOption = OptionInt;

            for (int i = 1; i < _options.Length; i++) {
                var optionIndex = (currentOption + i) % _options.Length;
                var option = _options[optionIndex];
                if (IsForbidden(option)) {
                    continue;
                }
                Option = option;
                onChangeByPlayer?.Invoke(_option);
                return;
            }
        }

        public void PreviousOption() {
            if (!CanChoosePrevOption) {
                return;
            }

            Option = Previous;
            onChangeByPlayer?.Invoke(_option);
        }
        
        bool IsForbidden(ToggleOption option) => _forbiddenOptions != null && _forbiddenOptions.Contains(option);
        
        public void EnsureCurrentOptionNotForbidden() {
            if (!IsForbidden(_options[OptionInt])) {
                return;
            }

            for (int i = OptionInt - 1; i >= 0; i--) {
                if (!IsForbidden(_options[i])) {
                    Option = _options[i];
                    return;
                }
            }
            for (int i = OptionInt + 1; i < _options.Length; i++) {
                if (!IsForbidden(_options[i])) {
                     Option = _options[i];
                    return;
                }
            }
        }
    }
}