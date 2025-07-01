using System;
using Awaken.TG.MVC.UI;
using UnityEngine;
using UnityEngine.UI;

namespace Awaken.TG.Main.UI.Components.PadShortcuts {
    public class SliderChangeAction : MonoBehaviour, IShortcutAction {
        public Slider slider;
        public float change;

        bool _wasActive;
        public bool Active => slider.interactable;
        public event Action OnActiveChange;

        public UIResult Invoke() {
            slider.value += change * (slider.maxValue - slider.minValue);
            return UIResult.Accept;
        }

        void Update() {
            if (Active != _wasActive) {
                _wasActive = Active;
                OnActiveChange?.Invoke();
            }
        }
    }
}