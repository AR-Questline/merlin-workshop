using Awaken.TG.Main.Locations.Attachments.Attachment;
using Sirenix.OdinInspector.Editor;
using UnityEditor;
using UnityEngine;

namespace Awaken.TG.Editor.Main.Locations {
    [CustomEditor(typeof(TheTowerOfBoneAndTimberCoordinatorAttachment))]
    public class TheTowerOfBoneAndTimberCoordinatorAttachmentEditor : OdinEditor {
        void OnSceneGUI() {
            var attachment = target as TheTowerOfBoneAndTimberCoordinatorAttachment;
            if (attachment == null) return;
            
            // Spawner position handle
            Handles.color = Color.cyan;
            EditorGUI.BeginChangeCheck();
            Vector3 newSpawnerPos = Handles.PositionHandle(attachment.spawnerPosition, Quaternion.identity);
            if (EditorGUI.EndChangeCheck()) {
                Undo.RecordObject(attachment, "Move Spawner Position");
                attachment.spawnerPosition = newSpawnerPos;
                EditorUtility.SetDirty(attachment);
            }
            Handles.Label(attachment.spawnerPosition + Vector3.up * 5f, "Spawner Position", new GUIStyle {
                normal = new GUIStyleState { textColor = Color.cyan },
                fontSize = 12,
                fontStyle = FontStyle.Bold
            });
            
            // Shout position handle
            Handles.color = Color.red;
            EditorGUI.BeginChangeCheck();
            Vector3 newShoutPos = Handles.PositionHandle(attachment.shoutPosition, Quaternion.identity);
            if (EditorGUI.EndChangeCheck()) {
                Undo.RecordObject(attachment, "Move Shout Position");
                attachment.shoutPosition = newShoutPos;
                EditorUtility.SetDirty(attachment);
            }
            Handles.Label(attachment.shoutPosition + Vector3.up * 5f, "Shout Position", new GUIStyle {
                normal = new GUIStyleState { textColor = Color.red },
                fontSize = 12,
                fontStyle = FontStyle.Bold
            });
        }
    }
}

