using Awaken.TG.Graphics.VFX;
using UnityEditor;

namespace Awaken.TG.Editor.Graphics.VFX {
    [CanEditMultipleObjects]
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
            foreach (var t in targets) {
                if (t is LightController controller) {
                    controller.OnValidate();
                }
            }
        }
    }
}