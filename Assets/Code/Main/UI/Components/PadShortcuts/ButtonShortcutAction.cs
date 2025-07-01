using System;
using Awaken.TG.MVC.UI;
using Awaken.TG.MVC.UI.Events;
using UnityEngine;

namespace Awaken.TG.Main.UI.Components.PadShortcuts {
    public class ButtonShortcutAction : MonoBehaviour, IShortcutAction {
        public ARButton button;
        public bool Active => button.Interactable && button.gameObject.activeInHierarchy;
        bool _wasActiveInHierarchy;
        
        public event Action OnActiveChange;
        public event Action OnButtonShortcutAction;
        
        public UIResult Invoke() {
            OnButtonShortcutAction?.Invoke();
            return button.Handle(new UISubmitAction());
        }

        private void Start() {
            button.OnInteractableChange += interactable => OnActiveChange?.Invoke();
        }
        
        void Update(){
            if (_wasActiveInHierarchy != button.gameObject.activeInHierarchy) {
                _wasActiveInHierarchy = button.gameObject.activeInHierarchy;
                OnActiveChange?.Invoke();
            }
        }
    }
}