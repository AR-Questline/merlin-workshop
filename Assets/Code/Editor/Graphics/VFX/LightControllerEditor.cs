using Awaken.TG.Graphics.VFX;
using UnityEditor;
using UnityEngine;

namespace Awaken.TG.Graphics.VFX {
    [CustomEditor(typeof(LightController), true)]
    public class LightControllerEditor : Sirenix.OdinInspector.Editor.OdinEditor {
        protected override void OnEnable() {
            base.OnEnable();
            OnSelectionChanged();
        }

        protected override void OnDisable() {
            base.OnDisable();
            OnSelectionChanged();
        }
        
        void OnSelectionChanged() {
            ((LightController)target).OnValidate();
        }
    }
}