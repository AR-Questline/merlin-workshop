using Awaken.TG.Code.Utility;
using Awaken.TG.Main.Character;
using Awaken.TG.Main.Fights.NPCs;
using Awaken.TG.Main.Locations;
using Awaken.TG.MVC;
using Awaken.TG.Utility;
using UnityEngine;

namespace Awaken.TG.Main.AI.Movement {
    public class VCFlyingEnemy : ViewComponent<Location>, UnityUpdateProvider.IWithLateUpdateGeneric {
        const float MinFlyHeightPercent = 0.2f;
        const float MaxFlyHeightPercent = 2f;
        const float MaxDistanceToTarget = 1f;
        const float MaxDistanceToTargetSqr = MaxDistanceToTarget * MaxDistanceToTarget;
        const float MaxDistanceToTargetDuringAttack = 0.1f;
        const float MaxDistanceToTargetDuringAttackSqr = MaxDistanceToTargetDuringAttack * MaxDistanceToTargetDuringAttack;
        const float RandomMovementStrength = 0.2f;
        const float RandomMovementStrengthInAttack = 0.05f;
        const float MovementLerpSpeed = 5f;
        const float HeightLerpSpeed = 0.25f;
        const float RotationLerpSpeed = 360f;
        
        [SerializeField] float flyHeight = 1.5f;
        [SerializeField] Transform movementTransform;
        Transform _target;
        NpcElement _npc;

        float _randomTimeOffset;
        Vector3 _lastPosition;
        Quaternion _lastRotation;
        bool _inAttack;
        
        float MaxDistance => _inAttack ? MaxDistanceToTargetDuringAttack : MaxDistanceToTarget;
        float MaxDistanceSqr => _inAttack ? MaxDistanceToTargetDuringAttackSqr : MaxDistanceToTargetSqr;
        float RandomMovementStr => _inAttack ? RandomMovementStrengthInAttack : RandomMovementStrength;
        
        protected override void OnAttach() {
            _target = Target.ViewParent;
            _npc = Target.TryGetElement<NpcElement>();
            _randomTimeOffset = RandomUtil.UniformFloat(0f, 10f);
            
            _lastPosition = movementTransform.position;
            _lastRotation = movementTransform.rotation;

            _npc.ListenTo(ICharacter.Events.OnAttackRelease, () => _inAttack = true, this);
            _npc.ListenTo(ICharacter.Events.OnAttackRecovery, () => _inAttack = false, this);
            
            World.Services.Get<UnityUpdateProvider>().RegisterLateGeneric(this);
        }
        
        protected override void OnDiscard() {
            World.Services.Get<UnityUpdateProvider>().UnregisterLateGeneric(this);
        }

        public void UnityLateUpdate(float deltaTime) {
            if (_npc is not { HasBeenDiscarded: false, IsAlive: true } || deltaTime <= 0f) {
                return;
            }

            var targetPosition = _target.position;
            
            var offset = _lastPosition - targetPosition;
            float height = offset.y;
            var positionVector = new Vector2(offset.x, offset.z);
            
            float movementDelta = deltaTime * MovementLerpSpeed;
            float heightDelta = deltaTime * HeightLerpSpeed;
            
            // height
            height = Mathf.Lerp(height, flyHeight, heightDelta);
            height = Mathf.Clamp(height, flyHeight * MinFlyHeightPercent, flyHeight * MaxFlyHeightPercent);
            
            // position
            positionVector = Vector3.Lerp(positionVector, Vector3.zero, movementDelta);
            if (positionVector.sqrMagnitude > MaxDistanceSqr) {
                positionVector = positionVector.normalized * MaxDistance;
            }

            _lastPosition = targetPosition + new Vector3(positionVector.x, height, positionVector.y);
            movementTransform.position = _lastPosition;

            // rotation
            var lookAtPosition = targetPosition + (_target.forward * 3);
            var targetRotation = Quaternion.LookRotation((lookAtPosition - _lastPosition).ToHorizontal3());
            
            movementTransform.rotation = Quaternion.Lerp(_lastRotation, targetRotation, deltaTime * RotationLerpSpeed);
            _lastRotation = movementTransform.rotation;
            
            //randomness
            movementTransform.localPosition += RandomizePosition() * RandomMovementStr;
        }

        Vector3 RandomizePosition() {
            const float VerticalMultiplier = 0.3f;
            const float VerticalSpeedMultiplier = 0.7f;
            const float VerticalSpeedSin = 17f * VerticalSpeedMultiplier;
            const float VerticalSpeedCos = 7f * VerticalSpeedMultiplier;
            const float HorizontalSpeedMultiplier = 0.33f;
            const float HorizontalSpeedSin = 7f * HorizontalSpeedMultiplier;
            const float HorizontalSpeedCos = 23f * HorizontalSpeedMultiplier;
            
            float time = Time.time + _randomTimeOffset;
            
            float y = Mathf.Cos(time * VerticalSpeedCos) / (2 + Mathf.Sin(time * VerticalSpeedSin));
            float x = Mathf.Sin(time * HorizontalSpeedSin) + Mathf.Cos(time * HorizontalSpeedCos);

            return new Vector3(x, y * VerticalMultiplier, 0);
        }
    }
}
