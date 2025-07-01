using System;
using Awaken.TG.MVC.UI;
using UnityEngine;
using UnityEngine.UI;

namespace Awaken.TG.Main.UI.Components.PadShortcuts {
    public class ToggleShortcutAction : MonoBehaviour, IShortcutAction {
        public Toggle toggle;

        bool _wasActive;
        public bool Active => toggle.interactable;
        public event Action OnActiveChange;

        public UIResult Invoke() {
            toggle.isOn = !toggle.isOn;
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