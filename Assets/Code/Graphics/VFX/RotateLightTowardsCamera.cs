using UnityEngine;

namespace Awaken.TG.Graphics.VFX {
    [ExecuteInEditMode]
    public class RotateObjectTowardsCamera : MonoBehaviour {
        public Transform targetCamera;
        public bool negate;
        public float distance = 50f;


        void Start() {
            if (targetCamera == null && Camera.main != null) {
                targetCamera = Camera.main.transform;
            }
        }

        void Update() {
#if UNITY_EDITOR
            if (targetCamera == null) {
                targetCamera = UnityEditor.SceneView.lastActiveSceneView?.camera?.transform;
            }
#endif
            if (targetCamera == null) {
                return;
            }

            Vector3 targetCameraForward = targetCamera.forward;
            Vector3 newPosition = targetCamera.position + targetCameraForward * distance;
            Transform lightTransform = transform;
            lightTransform.position = new Vector3(newPosition.x, lightTransform.position.y, newPosition.z);

            Vector3 lookDirection = targetCameraForward;
            if (negate) {
                lookDirection = -targetCamera.forward;
            }

            Vector3 eulerRotation = lightTransform.rotation.eulerAngles;
            lightTransform.rotation = Quaternion.Euler(eulerRotation.x, Quaternion.LookRotation(lookDirection).eulerAngles.y,
                eulerRotation.z);
        }
    }
}