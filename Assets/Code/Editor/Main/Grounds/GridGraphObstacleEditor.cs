using Awaken.TG.Main.Grounds;
using UnityEditor;
using UnityEngine;

namespace Awaken.TG.Editor.Main.Grounds {
    [CustomEditor(typeof(GridGraphObstacle))]
    public class GridGraphObstacleEditor : UnityEditor.Editor {
        GridGraphObstacle Target => (GridGraphObstacle) target;
        GameObject TargetGameObject => Target.gameObject;

        public override void OnInspectorGUI() {
            EditorGUILayout.HelpBox("This script takes care of managing NavMesh Cut. " +
                                    "If you want to modify nav collider, modify just the collider component," +
                                    "leave NavMeshCut alone if there is one.", MessageType.Info);
            if (Application.isPlaying) {
                EditorGUILayout.HelpBox("Collider type can't be changed in PlayMode", MessageType.Info);
                return;
            }
            int layer = LayerMask.NameToLayer("NavMeshObstacle");
            if (TargetGameObject.layer != layer) {
                TargetGameObject.layer = layer;
                EditorUtility.SetDirty(TargetGameObject);
            }
            
            EditorGUI.BeginChangeCheck();
            Target.colliderType = (ArColliderType) EditorGUILayout.EnumPopup("Collider type:", Target.colliderType);
            if (EditorGUI.EndChangeCheck()) {
                AddColliderComponent(TargetGameObject);
                EditorUtility.SetDirty(TargetGameObject);
            }
        }

        void AddColliderComponent(GameObject gameObject) {
            foreach (var collider in gameObject.GetComponents<Collider>()) {
                DestroyImmediate(collider);
            }
            Collider c;
            switch (Target.colliderType) {
                case ArColliderType.Box:
                    c = gameObject.AddComponent<BoxCollider>();
                    break;
                case ArColliderType.Capsule:
                    c = gameObject.AddComponent<CapsuleCollider>();
                    break;
                case ArColliderType.Sphere:
                    c = gameObject.AddComponent<SphereCollider>();
                    break;
                default:
                    c = gameObject.AddComponent<BoxCollider>();
                    break;
            }
            c.isTrigger = false;
        }
    }
}