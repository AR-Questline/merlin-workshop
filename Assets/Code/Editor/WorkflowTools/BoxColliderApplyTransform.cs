using UnityEditor;
using UnityEngine;

namespace Awaken.TG.Editor.WorkflowTools {
    public static class BoxColliderApplyTransform {
        [MenuItem("CONTEXT/BoxCollider/Apply Transform")]
        static void ApplyTransform(MenuCommand command) {
            BoxCollider collider = (BoxCollider) command.context;
            Transform colliderTransform = collider.transform;
            
            Undo.RecordObject(collider.gameObject, "Apply Transform");

            colliderTransform.position = colliderTransform.TransformPoint(collider.center);
            collider.center = Vector3.zero;
            collider.size = Vector3.Scale(collider.size, colliderTransform.localScale);
            colliderTransform.localScale = Vector3.one;
        }
    }
}