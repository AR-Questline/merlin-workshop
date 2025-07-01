using UnityEngine;

namespace Awaken.TG.Main.UI.TitleScreen.Loading {
    public class AnimateLoadingIcon : MonoBehaviour {
        Transform _myTransform;

        void Awake() {
            _myTransform = transform;
        }

        void Update() {
            _myTransform.Rotate(0, 0, -120 * Time.unscaledDeltaTime, Space.Self);
        }
    }
}
