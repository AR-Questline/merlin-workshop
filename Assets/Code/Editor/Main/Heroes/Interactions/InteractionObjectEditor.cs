using Awaken.TG.Main.Heroes.Interactions;
using Sirenix.OdinInspector.Editor;
using UnityEditor;
using UnityEngine;

namespace Awaken.TG.Editor.Main.Heroes.Interactions {
    [CustomEditor(typeof(InteractionObject))]
    public class InteractionObjectEditor : OdinEditor {
        const string DefaultTag = "Untagged";
        static readonly string Message = $"Object not ready for interactions. Set tag to {InteractionObject.InteractionTag}";
        
        InteractionObject Target => target as InteractionObject;
        bool Untagged => Target.CompareTag(DefaultTag);
        bool HasInteractionTag => Target.CompareTag(InteractionObject.InteractionTag);
        
        public override void OnInspectorGUI() {
            if (!HasInteractionTag) {
                EditorGUILayout.HelpBox(Message, MessageType.Warning);
            }
            
            if (Untagged && GUILayout.Button("Fix")) {
                PrepareForInteraction();
            }

            base.OnInspectorGUI();
        }
        
        void Reset() {
            PrepareForInteraction();
        }

        void PrepareForInteraction() {
            if (Untagged) {
                InteractionObject.SetupForInteraction(Target);
                EditorUtility.SetDirty(Target);
            }
        }
    }
}