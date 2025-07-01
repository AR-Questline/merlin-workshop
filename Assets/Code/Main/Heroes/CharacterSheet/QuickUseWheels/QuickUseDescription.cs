using System;
using TMPro;
using UnityEngine;

namespace Awaken.TG.Main.Heroes.CharacterSheet.QuickUseWheels {
    [Serializable]
    public struct QuickUseDescription {
        [SerializeField] CustomAction customAction;
        
        public void ShowCustomAction(string actionName, string actionDescription) => customAction.Show(actionName, actionDescription);
        
        public void HideCustomAction(string actionName) {
            if (actionName != null && actionName == customAction.CurrentActionName) {
                customAction.Hide();
            }
        }

        public void HideAll() {
            customAction.Hide();
        }

        [Serializable]
        struct CustomAction {
            [SerializeField] GameObject parent;
            [SerializeField] TextMeshProUGUI name;
            [SerializeField] TextMeshProUGUI description;
            public string CurrentActionName { get; private set; }
            
            public void Show(string actionName, string actionDescription) {
                CurrentActionName = actionName;
                name.SetText(actionName);
                description.SetText(actionDescription);
                parent.SetActive(true);
            }

            public void Hide() {
                parent.SetActive(false);
                CurrentActionName = null;
            }
        }
    }
}