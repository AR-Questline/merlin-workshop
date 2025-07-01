using System;
using Awaken.TG.Main.UI.Components;
using Awaken.TG.Main.Utility;
using Awaken.TG.MVC;
using Awaken.TG.MVC.Attributes;
using Awaken.TG.MVC.UI;
using Awaken.TG.MVC.UI.Events;
using Awaken.TG.MVC.UI.Handlers.Focuses;
using Awaken.TG.MVC.UI.Handlers.Hovers;
using UnityEngine;
using UnityEngine.UI;

namespace Awaken.TG.Main.Settings.GammaSettingScreen {
    [UsesPrefab("Settings/GammaScreen/" + nameof(VGammaSlider))]
    public class VGammaSlider : View<GammaScreen>, IHoverableView {
        [SerializeField] Slider slider;
        [SerializeField] ARButton buttonPlus;
        [SerializeField] ARButton buttonMinus;

        float _changePerStep;
        
        bool Interactable => slider.interactable;
        float MinValue => slider.minValue;
        float MaxValue => slider.maxValue;

        public void Setup(float minValue, float maxValue, bool wholeNumbers, float defaultValue, float stepChange, Action<float> onValueChanged = null, bool interactable = true) {
            slider.wholeNumbers = wholeNumbers;
            slider.minValue = minValue;
            slider.maxValue = maxValue;
            _changePerStep = stepChange;
            slider.interactable = interactable;
            buttonPlus.Interactable = interactable;
            buttonMinus.Interactable = interactable;
            slider.SetValueWithoutNotify(defaultValue);
            
            buttonPlus.OnClick += MoveSliderPlus;
            buttonMinus.OnClick += MoveSliderMinus;
            slider.onValueChanged.AddListener(OnChanged);
            slider.onValueChanged.AddListener(value => onValueChanged?.Invoke(value));
            World.Only<Focus>().Select(slider);
        }

        public UIResult Handle(UIEvent evt) {
            if ((evt is UIKeyDownAction action1 && action1.Name == KeyBindings.UI.Generic.IncreaseValue) ||
                (evt is UIKeyLongHeldAction holdAction1 && holdAction1.Name == KeyBindings.UI.Generic.IncreaseValue)) {
                MoveSliderPlus();
                return UIResult.Accept;
            }

            if ((evt is UIKeyDownAction action2 && action2.Name == KeyBindings.UI.Generic.DecreaseValue) ||
                (evt is UIKeyLongHeldAction holdAction2 && holdAction2.Name == KeyBindings.UI.Generic.DecreaseValue)) {
                MoveSliderMinus();
                return UIResult.Accept;
            }

            return UIResult.Ignore;
        }

        void OnChanged(float value) {
            buttonMinus.Interactable = Interactable && !Mathf.Approximately(value, MinValue);
            buttonPlus.Interactable = Interactable && !Mathf.Approximately(value, MaxValue);
        }

        void MoveSliderPlus() {
            MoveSlider(1);
        }

        void MoveSliderMinus() {
            MoveSlider(-1);
        }

        void MoveSlider(float sign) {
            slider.value += sign * _changePerStep;
        }
    }
}