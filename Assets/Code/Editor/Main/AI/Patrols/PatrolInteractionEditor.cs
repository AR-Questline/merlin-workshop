using Awaken.TG.Main.AI.Idle.Interactions.Patrols;
using Sirenix.OdinInspector.Editor;
using UnityEditor;

namespace Awaken.TG.Editor.Main.AI.Patrols {
    [CustomEditor(typeof(PatrolInteraction))]
    public class PatrolInteractionEditor : OdinEditor {
        int _selected = -1;
        
        ref PatrolPath Path => ref ((PatrolInteraction) target).PatrolPath;

        void OnPreSceneGUI() {
            if (PatrolPathEditor.OnPreSceneGUI(target, ref Path, ref _selected)) {
                Repaint();
            }
        }

        void OnSceneGUI() {
            if (PatrolPathEditor.OnSceneGUI(target, ref Path, ref _selected)) {
                Repaint();
            }
        }
        
        protected override void OnDisable() {
            PatrolPathEditor.OnDisable(ref Path, ref _selected);
            base.OnDisable();
        }
    }
}