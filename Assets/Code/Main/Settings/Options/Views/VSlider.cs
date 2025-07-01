using Awaken.TG.Main.Localization;
using Awaken.TG.Main.UI.ButtonSystem;
using Awaken.TG.Main.UI.Components;
using Awaken.TG.Main.Utility;
using Awaken.TG.Main.Utility.UI.Keys;
using Awaken.TG.MVC.Attributes;
using Awaken.TG.MVC.UI;
using Awaken.TG.MVC.UI.Events;
using Awaken.TG.MVC.UI.Handlers.Focuses;
using Awaken.TG.Utility;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Awaken.TG.Main.Settings.Options.Views {
    [UsesPrefab("Settings/VSlider")]
    public class VSlider : VFocusableSetting<SliderOption> {
        const float TickPeriod = 0.3f;

        [SerializeField] TMP_Text displayText;
        [SerializeField] Slider slider;
        [SerializeField] TMP_Text valueDisplay;
        [SerializeField] ARButton buttonPlus;
        [SerializeField] ARButton buttonMinus;

        Prompt _left;
        Prompt _right;
        NaviDirection _direction;
        float _holdTime;
        float _changePerStep;

        // === VFocusableSetting
        public override void Setup(PrefOption option) {
            base.Setup(option);

            Refresh();

            displayText.text = Option.DisplayName;
            slider.wholeNumbers = Option.WholeNumbers;
            slider.minValue = Option.MinValue;
            slider.maxValue = Option.MaxValue;
            _changePerStep = Option.ChangePerStep;
            slider.SetValueWithoutNotify(Option.Value);

            buttonPlus.OnClick += MoveSliderPlus;
            buttonMinus.OnClick += MoveSliderMinus;

            slider.onValueChanged.AddListener(v => Option.Value = v);
            Option.onChange += OnChange;
            OnChange(Option.Value);
        }

        protected override void RemovePrompts() {
            Target.Prompts.RemovePrompt(ref _left);
            Target.Prompts.RemovePrompt(ref _right);
        }
        
        protected override void SpawnPrompts() {
            _left = Target.Prompts.AddPrompt(
                Prompt.VisualOnlyTap(KeyBindings.Gamepad.DPad_Left, LocTerms.SettingsSliderMinus.Translate(),
                    controllers: ControlSchemeFlag.Gamepad), Target);
            _right = Target.Prompts.AddPrompt(
                Prompt.VisualOnlyTap(KeyBindings.Gamepad.DPad_Right, LocTerms.SettingsSliderPlus.Translate(),
                    controllers: ControlSchemeFlag.Gamepad), Target);
        }
        
        protected override void Cleanup() {
            Option.onChange -= OnChange;
            buttonPlus.OnClick -= MoveSliderPlus;
            buttonMinus.OnClick -= MoveSliderMinus;
        }
        
        protected override void Refresh() {
            slider.interactable = Option.Interactable;
            buttonPlus.Interactable = Option.Interactable;
            buttonMinus.Interactable = Option.Interactable;
        }

        // === UI handling
        public override UIResult Handle(UIEvent evt) {
            if (evt is UINaviAction navi) {
                if (navi.direction == NaviDirection.Left) {
                    MoveSlider(-1f);
                    return UIResult.Prevent;
                }
                if (navi.direction == NaviDirection.Right) {
                    MoveSlider(1f);
                    return UIResult.Prevent;
                }
            } else if (evt is UIKeyHeldAction action) {
                return HandleKeyHeld(action);
            } else if (evt is UIKeyDownAction downAction) {
                if (downAction.Name == KeyBindings.Gamepad.DPad_Left || downAction.Name == KeyBindings.Gamepad.DPad_Right) {
                    _holdTime = 0f;
                    _direction = null;
                }
            }

            return UIResult.Ignore;
        }

        UIResult HandleKeyHeld(UIKeyHeldAction action) {
            UIResult result = UIResult.Ignore;

            if (action.Name == KeyBindings.Gamepad.DPad_Left) {
                if (_direction != NaviDirection.Left) {
                    _direction = NaviDirection.Left;
                    _holdTime = 0f;
                }

                _holdTime += Time.unscaledDeltaTime;
                result = UIResult.Accept;
            } else if (action.Name == KeyBindings.Gamepad.DPad_Right) {
                if (_direction != NaviDirection.Right) {
                    _direction = NaviDirection.Right;
                    _holdTime = 0f;
                }

                _holdTime += Time.unscaledDeltaTime;
                result = UIResult.Accept;
            }

            if (_holdTime > TickPeriod) {
                float sign = _direction == NaviDirection.Right ? 1f : -1f;
                MoveSlider(sign);
                _holdTime = 0f;
                _direction = null;
            }

            return result;
        }

        // === Helpers
        void OnChange(float value) {
            slider.SetValueWithoutNotify(Option.Value);
            valueDisplay.text = Option.DisplayValue;
            buttonMinus.Interactable = Option.Interactable && !Mathf.Approximately(value, Option.MinValue);
            buttonPlus.Interactable = Option.Interactable && !Mathf.Approximately(value, Option.MaxValue);
        }
        
        void MoveSliderPlus() {
            MoveSlider(1);
        }
        
        void MoveSliderMinus() {
            MoveSlider(-1);
        }
        
        void MoveSlider(float sign) {
            Option.Value += sign * _changePerStep;
        }
    }
}