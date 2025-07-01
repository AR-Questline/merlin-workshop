using Awaken.TG.Main.Scenes.SceneConstructors;
using UnityEngine;

namespace Awaken.TG.Debugging {
    public class DebugCuanachtCutsceneDisabler : MonoBehaviour {
        void Start() {
            if (DebugReferences.DisableCuanachtCutscene) {
                gameObject.SetActive(false);
            }
        }
    }
}