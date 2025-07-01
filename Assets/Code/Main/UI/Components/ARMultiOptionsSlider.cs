using System;
using Awaken.TG.Main.UI.Popup.PopupContents;
using Awaken.TG.MVC.UI;
using Awaken.TG.MVC.UI.Events;
using UnityEngine;

namespace Awaken.TG.Main.UI.Components {
    public class ARMultiOptionsSlider : MonoBehaviour, IUIAware {
        [SerializeField] QuantitySlider slider;
        [SerializeField] ARMultiOptions arMultiOptions;
        
        event Action<float> ValueSetter;
        float _changePerStep;
        public float Value => slider.value;

        public void Initialize(string name, Action<float> valueSetter, float min = 0f, float max = 100f, float step = 1f, float initialValue = 0f) {
            ValueSetter = valueSetter;
            _changePerStep = step;
            slider.wholeNumbers = false;
            slider.minValue = min;
            slider.maxValue = max;
            slider.onValueChanged.AddListener(val => {
                ValueSetter?.Invoke(val);
            });
            slider.value = initialValue;
            
            arMultiOptions.Initialize(name, MoveSliderMinus, MoveSliderPlus);
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

        public UIResult Handle(UIEvent evt) {
            return arMultiOptions.Handle(evt);
        }
    }
}