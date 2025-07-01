using System.Linq;
using Awaken.TG.Editor.Main.Templates;
using Awaken.TG.Main.Locations.Setup;
using Awaken.Utility.Extensions;
using UnityEditor;
using UnityEngine;

namespace Awaken.TG.Editor.Main.Locations.Setup {
    [CustomEditor(typeof(LocationSpec))]
    public class LocationSpecEditor : BasePresetsEditor {
        LocationSpec Spec => (LocationSpec)target;
        
        public override void OnInspectorGUI() {
            if (!Spec.gameObject.hideFlags.HasFlagFast(HideFlags.DontSaveInEditor)) {
                ShowId();
            }

            AttachmentGroup[] groups = Spec.gameObject.GetComponentsInChildren<AttachmentGroup>();
            if (groups.Length > 0) {
                string groupsString = string.Join(", ", groups.Select(group => group.name));
                EditorGUILayout.HelpBox($"Attachment Groups: {groupsString}", MessageType.Info);
            }

            base.OnInspectorGUI();
        }

        void ShowId() {
            bool previousState = GUI.enabled;
            GUI.enabled = false;
            EditorGUILayout.TextField("ID: ", Spec.SceneId.FullId);
            GUI.enabled = previousState;
        }
    }
}