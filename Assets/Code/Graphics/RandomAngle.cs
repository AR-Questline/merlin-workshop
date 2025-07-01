using Awaken.TG.Graphics.VFX;
using UnityEngine;
using Random = UnityEngine.Random;

namespace Awaken.TG.Graphics {
    [ExecuteAlways]
    public class RandomAngle : MonoBehaviour {
        [SerializeField] float minAngle = -60f;
        [SerializeField] float maxAngle = 60f;
        [SerializeField] float rotationSpeed = 1f;
        
        Quaternion _targetRotation;

        void Start() {
            CalculateNextTargetRotation();
        }

        void Update() {
            if (!LightController.EditorPreviewUpdates) return;
            var currentRotation = transform.localRotation;
            if (Quaternion.Angle(currentRotation, _targetRotation) <= 0.1f) {
                CalculateNextTargetRotation();
            }
            transform.localRotation = Quaternion.Slerp(currentRotation, _targetRotation, rotationSpeed * Time.deltaTime);
        }

        void CalculateNextTargetRotation() {
            _targetRotation = Quaternion.Euler(Random.Range(minAngle, maxAngle), 0, Random.Range(minAngle, maxAngle));
        }
    }
}