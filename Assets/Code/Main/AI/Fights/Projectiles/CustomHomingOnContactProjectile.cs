using Awaken.TG.Main.Character;
using Awaken.TG.Utility;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Awaken.TG.Main.AI.Fights.Projectiles {
    public class CustomHomingOnContactProjectile : CustomOnContactProjectile {
        const string HomingSettingsGroup = "Homing Missile Settings";
        [SerializeField, FoldoutGroup(HomingSettingsGroup)] float homingStrength = 1f;
        [SerializeField, FoldoutGroup(HomingSettingsGroup)] float velocityScalarLimit = 1f;
        [SerializeField, FoldoutGroup(HomingSettingsGroup)] float increaseHomingStrengthOnDistance = 3f;
        [SerializeField, FoldoutGroup(HomingSettingsGroup)] float homingStrengthMultiplierWhenClose = 5f;
        
        float _baseSpeed;
        ICharacter _target;
        
        public void SetTarget(ICharacter target) {
            _target = target;
        }
        
        public override void SetVelocityAndForward(Vector3 velocity, ProjectileOffsetData? offsetData = null) {
            base.SetVelocityAndForward(velocity, offsetData);
            _baseSpeed = _rb.linearVelocity.magnitude;
        }
        
        protected override void ProcessFixedUpdate(float deltaTime) {
            base.ProcessFixedUpdate(deltaTime);

            if (!_isSetup || _rb.isKinematic) {
                return;
            }
            
            if (TryGetTargetPosition(out var targetPosition)) {
                ApplyHoming(targetPosition);
            }
        }
        
        void ApplyHoming(Vector3 targetPosition) {
            var homingDirection = (targetPosition - _rb.position).normalized;
            var homingForce = _baseSpeed * homingStrength;
            
            // --- increase homing on closer distance ---
            var distance = Vector3.Distance(_rb.position, targetPosition);
            var distanceHomingStrength = distance.Remap(increaseHomingStrengthOnDistance, 0, 1, homingStrengthMultiplierWhenClose, true);
            homingForce *= distanceHomingStrength;
            
            _rb.AddForce(homingDirection * homingForce, ForceMode.Force);
            
            float maxSpeed = _baseSpeed * velocityScalarLimit;
            _rb.linearVelocity = Vector3.ClampMagnitude(_rb.linearVelocity, maxSpeed);
        }
        
        protected bool TryGetTargetPosition(out Vector3 targetPosition) {
            if (_target is { HasBeenDiscarded: false }) {
                targetPosition = _target.Coords + Vector3.up * _target.Height * 0.8f;
                return true;
            }
            targetPosition = default;
            return false;
        }
    }
}