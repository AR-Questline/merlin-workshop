using Sirenix.OdinInspector;
using UnityEngine;

namespace Awaken.TG.LeshyRenderer {
    public class LeshyObjectSettings : MonoBehaviour {
        const string DefaultColliderGOName = "Colliders";
        
#if UNITY_EDITOR
        [InfoBox("This type of prefab should probably have a collider", InfoMessageType.Warning, nameof(ShowColliderWarning))]
#endif
        public GameObject collidersGO;
        public LeshyPrefabs.PrefabType prefabType = LeshyPrefabs.PrefabType.Tree;
        [Min(0)] public float lodFactor = 1;
        [Min(0)] public float colliderDistanceFactor = 1;
        public bool useBillboards = true;
        public float distanceFalloff = 0;

#if UNITY_EDITOR
        bool ShowColliderWarning => prefabType == LeshyPrefabs.PrefabType.Tree || prefabType == LeshyPrefabs.PrefabType.LargeObject;
        void Reset() {
            if (collidersGO == null && TryFindChildWithName(DefaultColliderGOName, out collidersGO)) {
                UnityEditor.EditorUtility.SetDirty(gameObject);
            }
        }

        bool TryFindChildWithName(string childName, out GameObject result) {
            int childCount = transform.childCount;
            for (int i = 0; i < childCount; i++) {
                var child = transform.GetChild(i);
                if (child.name == childName) {
                    result = child.gameObject;
                    return true;
                }
            }
            result = null;
            return false;
        }
#endif
    }
}