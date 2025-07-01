using System.Linq;
using UnityEditor;
using UnityEngine;

namespace Awaken.TG.Editor.WorkflowTools {
    public static class GroupGameObjectsUtility {
        [MenuItem("GameObject/TG/Group Selected GameObjects %g")]
        public static void GroupSelectedGameObjects() {
            var selected = Selection.gameObjects;
            if (selected.Any()) {
                var parent = new GameObject("Group");
                var parentTransform = parent.transform;
                var firstTransform = selected[0].transform;
                parentTransform.SetParent(firstTransform.parent);
                parentTransform.SetSiblingIndex(firstTransform.GetSiblingIndex());
                Undo.RegisterCreatedObjectUndo(parent, "Group Selected GameObjects");
                foreach (var go in selected) {
                    Undo.SetTransformParent(go.transform, parentTransform, "Change parent");
                }
                EditorGUIUtility.PingObject(firstTransform.gameObject);
            }
        }
    }
}