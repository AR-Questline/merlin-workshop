using UnityEngine;

namespace Awaken.TG.Main.AI.Movement.Controllers.Rotation {
    public class RotateTowards : IRotationScheme {
        public NpcController Controller { get; set; }
        public bool UseRichAIRotation => false;

        float _rotation;

        public RotateTowards(float rotation) {
            UpdateRotation(rotation);
        }

        public RotateTowards(Vector3 forward) {
            UpdateForward(forward);
        }

        public void Enter() { }

        public void Update(float deltaTime) {
            Controller.Rotation = _rotation;
        }

        public void UpdateRotation(float rotation) {
            _rotation = rotation;
        }

        public void UpdateForward(Vector3 forward) {
            UpdateRotation(Mathf.Atan2(forward.x, forward.z) * Mathf.Rad2Deg);
        }

        public void LookAt(Vector3 point) {
            if (Controller != null) { 
                UpdateForward(point - Controller.Position);
            }
        }
    }
}