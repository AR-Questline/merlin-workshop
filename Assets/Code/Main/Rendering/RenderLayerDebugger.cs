using Awaken.Utility.Debugging;
using Awaken.Utility.Enums;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.SceneManagement;
using LogType = Awaken.Utility.Debugging.LogType;

namespace Awaken.TG.Main.Rendering {
    public class RenderLayerDebugger : MonoBehaviour {
        [Button]
        void Find(LayerMask mask) {
            int sceneCount = SceneManager.sceneCount;
            for (int i = 0; i < sceneCount; i++) {
                foreach (var go in SceneManager.GetSceneAt(i).GetRootGameObjects()) {
                    CheckForLayer(go, mask);
                }
            }
        }

        static void CheckForLayer(GameObject go, LayerMask mask) {
            if (mask.Contains(go.layer)) {
                Log.Important?.Info($"GameObject matching mask found {go}", go);
            } else {
                foreach (Transform t in go.transform) {
                    CheckForLayer(t.gameObject, mask);
                }
            }
        }
    }
}