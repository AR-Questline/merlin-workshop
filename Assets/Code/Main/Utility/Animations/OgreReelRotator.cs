using UnityEngine;

namespace Awaken.TG.Main.Utility.Animations {
    public class OgreReelRotator : MonoBehaviour {
        [SerializeField] float rotationSpeed = -2.66f;

        void Update() {
            transform.Rotate(Vector3.up * (rotationSpeed * Time.deltaTime));
        }
    }
}
