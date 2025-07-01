using Awaken.TG.MVC;
using UnityEngine;

namespace Awaken.TG.Main.Cameras {
    public class MapCamera : MonoBehaviour, IService {
        public Camera MinimapCamera { [UnityEngine.Scripting.Preserve] get; private set; }

        private void Awake() {
            MinimapCamera = GetComponent<Camera>();
        }
    }
}
