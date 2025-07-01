using Awaken.TG.MVC;
using Sirenix.OdinInspector;
using System;
using UnityEngine;

namespace Awaken.TG.Main.Utility.UI {
    /// <summary>
    /// Simple view component that disables a set of objects on start.
    /// Useful for hiding UI elements that are only used for editor purposes.
    /// </summary>
    public class VCDisableOnStart : ViewComponent {
        const string SetupSection = "Setup";
        const string ActionsSection = "Actions";
        
        [BoxGroup(SetupSection), SerializeField] bool disableOnStart = true;
        [BoxGroup(SetupSection), SerializeField] bool self = true;
        [BoxGroup(SetupSection), SerializeField] bool allChildObjects;
        
        [BoxGroup(SetupSection), SerializeField] bool chooseObjs;
        [BoxGroup(SetupSection), SerializeField, ShowIf(nameof(chooseObjs))] GameObject[] disableObjs = Array.Empty<GameObject>();
        

        void Start() {
            if (disableOnStart) OverrideState(false);
        }

        void OverrideState(bool state) {
            if (allChildObjects) OverrideChildrenState(state);
            if (self) gameObject.SetActive(state);
            if (chooseObjs) OverrideChooseState(state);
        }
        
        void OverrideChildrenState(bool state) {
            foreach (Transform child in transform) {
                child.gameObject.SetActive(state);
            }
        }
        
        void OverrideChooseState(bool state) {
            foreach (GameObject obj in disableObjs) {
                obj.SetActive(state);
            }
        }

#if UNITY_EDITOR
        [BoxGroup(ActionsSection), Button("Disable objects")]
        void Disable() {
            OverrideState(false);
        }

        [BoxGroup(ActionsSection), Button("Enable objects")]
        void Enable() {
            OverrideState(true);
        }
#endif
    }
}
