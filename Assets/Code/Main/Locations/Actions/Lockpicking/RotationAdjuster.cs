using Awaken.TG.Utility.Maths;
using Awaken.Utility.Maths;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Awaken.TG.Main.Locations.Actions.Lockpicking {
    public class RotationAdjuster : MonoBehaviour {
        [SerializeField] Vector3 rotationAxis = Vector3.forward;
        [SerializeField] float speed = 180f;

        Vector3 _oldAltForward = Vector3.forward;

        float _destinationDegree;
        float _rotation;
        Transform _transform;

        void Awake() {
            _transform = transform;
        }

        void OnValidate() {
            AssureAxis();
        }

        void Update() {
            _rotation = Mathf.MoveTowardsAngle(_rotation, _destinationDegree, speed*Time.unscaledDeltaTime);
            _transform.localRotation = Quaternion.AngleAxis(_rotation, rotationAxis);
        }

        [Button]
        public void SetRotation(float degrees) {
            _destinationDegree = degrees;
        }

        void AssureAxis() {
            if (rotationAxis == _oldAltForward) return;
            if (rotationAxis == -_oldAltForward) {
                _oldAltForward = rotationAxis;
                return;
            }

            rotationAxis -= _oldAltForward;


            var selection = rotationAxis.Max();
            int finVal = 1;
            if (selection == 0) {
                selection = rotationAxis.Min();
                finVal = -1;
            }

            rotationAxis.Set(
                rotationAxis.x != selection ? 0 : finVal,
                rotationAxis.y != selection ? 0 : finVal,
                rotationAxis.z != selection ? 0 : finVal);
            _oldAltForward = rotationAxis;
        }
    }
}