using Awaken.CommonInterfaces.Assets;
using Awaken.Utility.Assets;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Awaken.TG.Main.Grounds {
    public class TerrainGroundBoundsBaker : MonoBehaviour, IEditorOnlyMonoBehaviour {
        [SerializeField] Impl impl;

        [Button]
        public void Bake(GroundBounds groundBounds) {
#if UNITY_EDITOR
            if (UnityEditor.PrefabUtility.IsPartOfPrefabInstance(gameObject)) {
                var root = UnityEditor.PrefabUtility.GetOutermostPrefabInstanceRoot(gameObject);
                UnityEditor.PrefabUtility.UnpackPrefabInstance(root, UnityEditor.PrefabUnpackMode.Completely,
                    UnityEditor.InteractionMode.AutomatedAction);
            }
#endif
            if (impl) {
                impl.Bake(groundBounds, this);
            }
        }
        
        public abstract class Impl : ScriptableObject {
            public abstract void Bake(GroundBounds groundBounds, TerrainGroundBoundsBaker baker);
        }
    }
}
