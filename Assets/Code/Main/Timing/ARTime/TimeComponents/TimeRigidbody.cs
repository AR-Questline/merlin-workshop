using UnityEngine;

namespace Awaken.TG.Main.Timing.ARTime.TimeComponents {
    public class TimeRigidbody : FactorBasedTimeComponent<Rigidbody> {
        Rigidbody _rigidbody;
        
        float _gravityAdjustment = 0;
        bool _wasKinematic;
        Vector3 _velocity;
        Vector3 _angularVelocity;

        public TimeRigidbody(Rigidbody rigidbody) {
            _rigidbody = rigidbody;
        }

        protected override void CacheBeforePause() {
            _velocity = _rigidbody.linearVelocity;
            _angularVelocity = _rigidbody.angularVelocity;
            _wasKinematic = _rigidbody.isKinematic;
            _rigidbody.isKinematic = true;
        }

        protected override void RestoreAfterPause() {
            _rigidbody.isKinematic = _wasKinematic;
            _rigidbody.linearVelocity = _velocity;
            _rigidbody.angularVelocity = _angularVelocity;
        }

        protected override void OnTimeScaleChangeNoPause(float from, float to, float factor) {
            if (_rigidbody.isKinematic) {
                return;
            }
            _rigidbody.linearVelocity *= factor;
            _rigidbody.angularVelocity *= factor;
            _rigidbody.linearDamping *= factor;
            _rigidbody.angularDamping *= factor;
            _gravityAdjustment = to * to - 1;
        }

        public override void OnFixedUpdate(float fixedDeltaTime) {
            if (_rigidbody.useGravity) {
                _rigidbody.AddForce(_rigidbody.mass * _gravityAdjustment * Physics.gravity);
            }
        }

        public override Component Component => _rigidbody;
    }
}