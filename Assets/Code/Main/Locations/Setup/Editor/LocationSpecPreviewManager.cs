#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Awaken.TG.Main.Locations.Setup.Editor {
    [InitializeOnLoad]
    public static class LocationSpecPreviewManager {
        static Dictionary<LocationSpec, GameObject> s_previews = new();

        public static void EDITOR_RuntimeReset() {
            s_previews.Clear();
        }
        
        static LocationSpecPreviewManager() {
            PrefabStage.prefabSaved += OnPrefabSaved;
        }

        static void OnPrefabSaved(GameObject gameObject) {
            HashSet<LocationSpec> destroyedSpecs = new();
            foreach ((LocationSpec owner, GameObject preview) in s_previews) {
                if (owner == null) {
                    if (preview != null) {
                        Object.DestroyImmediate(preview);
                    }
                    destroyedSpecs.Add(owner);
                }
            }
            foreach (LocationSpec destroyedSpec in destroyedSpecs) {
                s_previews.Remove(destroyedSpec);
            }
        }

        public static void RegisterPreview(LocationSpec owner, GameObject preview) {
            s_previews.Add(owner, preview);
        }

        public static void UnregisterPreview(LocationSpec owner) {
            s_previews.Remove(owner);
        }
    }
}
#endif