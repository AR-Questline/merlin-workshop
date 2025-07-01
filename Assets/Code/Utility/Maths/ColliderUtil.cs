using UnityEngine;

namespace Awaken.Utility.Maths {
    public static class ColliderUtil {
        public static bool IsPointWithinCollider(Collider collider, Vector3 point) {
            if (collider == null || !collider.gameObject.activeInHierarchy) {
                return false;
            }
            return collider.ClosestPoint(point) == point;
        }
        
        public static Vector3 ProjectCenterOntoLine(this Collider collider, Vector3 lineStart, Vector3 lineForward) {
            var colliderCenter = collider.bounds.center;
            var lineToCenter = colliderCenter - lineStart;
            var dot = Vector3.Dot(lineToCenter, lineForward);
            if (dot < 0) {
                return lineStart;
            }
            return lineStart + dot * lineForward;
        }

#if UNITY_EDITOR
        [UnityEditor.MenuItem("CONTEXT/Collider/Remove Invalid MeshColliders Here")]
        public static void RemoveCollidersWithNoMeshSet(UnityEditor.MenuCommand command) {
            Collider target = (Collider) command.context;
            try {
                UnityEditor.AssetDatabase.StartAssetEditing();
                foreach (MeshCollider meshCollider in target.transform.GetComponentsInChildren<MeshCollider>()) {
                    if (meshCollider.sharedMesh == null) {
                        UnityEditor.Undo.DestroyObjectImmediate(meshCollider);
                    }
                }
            } finally {
                UnityEditor.AssetDatabase.StopAssetEditing();
            }
        }
#endif
    }
}
