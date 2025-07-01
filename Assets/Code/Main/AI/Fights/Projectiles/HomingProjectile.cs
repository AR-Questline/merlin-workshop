using System.Collections.Generic;
using System.Linq;
using Awaken.TG.Main.AI.Grid;
using Awaken.TG.Main.AI.Utils;
using Awaken.TG.Main.Character;
using Awaken.TG.Main.Fights.NPCs;
using Awaken.TG.MVC;
using Awaken.TG.Utility;
using Sirenix.OdinInspector;
using Unity.Mathematics;
using UnityEngine;

namespace Awaken.TG.Main.AI.Fights.Projectiles {
    public class HomingProjectile : MagicProjectile {
        const string HomingSettingsGroup = "Homing Missile Settings";
        const string TargetingGroup = "Homing Targeting Settings";
        const string PenetrationGroup = "Homing Penetration Settings";
        
        [SerializeField, FoldoutGroup(HomingSettingsGroup)] float homingStrength = 1f;
        [SerializeField, FoldoutGroup(HomingSettingsGroup)] float velocityScalarLimit = 1f;
        
        [SerializeField, FoldoutGroup(HomingSettingsGroup)] bool weakenHomingOnDistanceRange = false;
        [ShowIf(nameof(weakenHomingOnDistanceRange)), Indent]
        [SerializeField, FoldoutGroup(HomingSettingsGroup)] float strongestHomingDistance = 5;
        [ShowIf(nameof(weakenHomingOnDistanceRange)), Indent]
        [SerializeField, FoldoutGroup(HomingSettingsGroup)] float weakestHomingDistance = 0;
        
        [SerializeField, FoldoutGroup(HomingSettingsGroup)] bool weakenHomingOnAngleRange = true;
        [ShowIf(nameof(weakenHomingOnAngleRange)), Indent]
        [SerializeField, FoldoutGroup(HomingSettingsGroup), Range(0f, 180f)] float strongestHomingAngle = 0f;
        [ShowIf(nameof(weakenHomingOnAngleRange)), Indent]
        [SerializeField, FoldoutGroup(HomingSettingsGroup), Range(0f, 180f)] float weakestHomingAngle = 90f;
        
        [SerializeField, FoldoutGroup(TargetingGroup)] TargetUpdateMethod findTargetOnStart = TargetUpdateMethod.TrySet;
        [SerializeField, FoldoutGroup(TargetingGroup)] TargetUpdateMethod findTargetOnDirectionChanged = TargetUpdateMethod.ReplaceOrClear;
        [SerializeField, FoldoutGroup(TargetingGroup)] TargetUpdateMethod findTargetAfterPenetration = TargetUpdateMethod.ReplaceOrClear;
        [SerializeField, FoldoutGroup(TargetingGroup)] float targetFindAngle = 50;
        [SerializeField, FoldoutGroup(TargetingGroup)] float targetFindDistance = 30;
        [SerializeField, FoldoutGroup(TargetingGroup)] bool findTargetsByCrosshairDistance = true;
        [SerializeField, FoldoutGroup(TargetingGroup)] bool findOnlyNonHitTargets = true;
        [SerializeField, FoldoutGroup(TargetingGroup)] bool targetEnemies = true;
        [SerializeField, FoldoutGroup(TargetingGroup)] bool targetNeutrals = false;
        [SerializeField, FoldoutGroup(TargetingGroup)] bool targetAllies = false;
        
        [SerializeField, FoldoutGroup(PenetrationGroup)] float penetrationBoostSpeed = 5f;
        [SerializeField, FoldoutGroup(PenetrationGroup)] float penetrationBoostUpSpeed = 1f;
        [SerializeField, FoldoutGroup(PenetrationGroup)] float penetrationBoostDuration = 0.2f;
        [SerializeField, FoldoutGroup(PenetrationGroup)] bool allowHittingTargetAgainAfterPenetration = false;

        ICharacter _characterTarget; 
        Transform _target;
        float _aimAtHeight;
        
        float _baseSpeed;
        float _penetrationBoostRemainingTime;
        
        float _cosStrongestHomingAngle;
        float _cosWeakestHomingAngle;
        float _targetFindAngleRadians;

        protected ICharacter HomingCharacterTarget => _characterTarget;

        protected override void OnSetup(Transform firePoint) {
            base.OnSetup(firePoint);
            _cosStrongestHomingAngle = math.cos(strongestHomingAngle * math.TORADIANS);
            _cosWeakestHomingAngle = math.cos(weakestHomingAngle * math.TORADIANS);
            _targetFindAngleRadians = targetFindAngle * math.TORADIANS;
        }
        
        protected override void OnFullyConfigured() {
            base.OnFullyConfigured();
            TrySearchForNewTarget(findTargetOnStart);
        }

        public virtual void SetTarget(ICharacter target, float aimAtHeight = 0.5f) {
            SetFixedTarget(target?.Torso);
            _characterTarget = target;
            _aimAtHeight = aimAtHeight;
        }

        public void SetFixedTarget(Transform target) {
            _target = target;
        }

        public void ResetTarget() {
            _characterTarget = null;
            _target = null;
            _aimAtHeight = 0.0f;
        }

        public override void SetVelocityAndForward(Vector3 velocity, ProjectileOffsetData? offsetData = null) {
            base.SetVelocityAndForward(velocity, offsetData);
            _baseSpeed = _rb.linearVelocity.magnitude;
            
            if (_initialized) {
                TrySearchForNewTarget(findTargetOnDirectionChanged);
            }
        }

        protected override void ProcessFixedUpdate(float deltaTime) {
            base.ProcessFixedUpdate(deltaTime);

            if (!_isSetup || _rb.isKinematic) {
                return;
            }

            if (_penetrationBoostRemainingTime > 0) {
                ApplyPenetrationBoost();
                _penetrationBoostRemainingTime -= deltaTime;

                if (_penetrationBoostRemainingTime <= 0) {
                    _penetrationBoostRemainingTime = 0;
                    OnAfterPenetrationBoost();
                }
                
                return;
            }

            if (TryGetTargetPosition(out var targetPosition)) {
                ApplyHoming(targetPosition);
            }
        }

        void ApplyPenetrationBoost() {
            var penetrationBoostForce = transform.forward.ToHorizontal3() * penetrationBoostSpeed;
            var penetrationBoostUpForce = Vector3.up * penetrationBoostUpSpeed;
            _rb.AddForce(penetrationBoostForce + penetrationBoostUpForce, ForceMode.VelocityChange);
        }
        
        void ApplyHoming(Vector3 targetPosition) {
            var homingDirection = (targetPosition - _rb.position).normalized;
            var homingForce = _baseSpeed * homingStrength;
            
            if (weakenHomingOnDistanceRange) {
                var distance = Vector3.Distance(_rb.position, targetPosition);
                var distanceHomingStrength = distance.RemapTo01(weakestHomingDistance, strongestHomingDistance, true);
                homingForce *= distanceHomingStrength;
            }
            
            if (weakenHomingOnAngleRange) {
                var dotToHomingTarget = Vector3.Dot(homingDirection, _rb.linearVelocity.normalized);
                var angledHomingStrength = dotToHomingTarget.RemapTo01(_cosWeakestHomingAngle, _cosStrongestHomingAngle, true);
                homingForce *= angledHomingStrength;
            }
            
            _rb.AddForce(homingDirection * homingForce, ForceMode.Force);
            
            float maxSpeed = _baseSpeed * velocityScalarLimit;
            _rb.linearVelocity = Vector3.ClampMagnitude(_rb.linearVelocity, maxSpeed);
        }

        bool TryGetTargetPosition(out Vector3 targetPosition) {
            if (_target != null) {
                targetPosition = _target.position;
                return true;
            }
            if (_characterTarget is { HasBeenDiscarded: false }) {
                targetPosition = _characterTarget.Coords + Vector3.up * _aimAtHeight;
                return true;
            }
            targetPosition = default;
            return false;
        }
        
        protected override void OnTargetPenetrated(IAlive alive) {
            StartPenetrationBoost();
        }
        
        void StartPenetrationBoost() {
            _penetrationBoostRemainingTime = penetrationBoostDuration;
        }

        void OnAfterPenetrationBoost() {
            if (allowHittingTargetAgainAfterPenetration) {
                _alivesHit.Clear();
            }
            TrySearchForNewTarget(findTargetAfterPenetration);
        }
        
        protected void TrySearchForNewTarget(TargetUpdateMethod updateMethod = TargetUpdateMethod.TrySet) {
            if (updateMethod == TargetUpdateMethod.None) {
                return;
            }
            
            if (updateMethod == TargetUpdateMethod.TrySet && _characterTarget != null) {
                return;
            }
            
            var newTarget = FindTarget();
            if (newTarget != null) {
                SetTarget(newTarget);
            } else if (updateMethod == TargetUpdateMethod.ReplaceOrClear) {
                ResetTarget();
            }
        }
        
        NpcElement FindTarget() {
            var searchRay = new Ray(_transform.position, _transform.forward);
            
            var npcGrid = World.Services.Get<NpcGrid>();
            IEnumerable<NpcElement> npcs = npcGrid.GetNpcsInCone(searchRay.origin, searchRay.direction, targetFindDistance, _targetFindAngleRadians);

            if (findOnlyNonHitTargets) {
                npcs = npcs.Where(npc => !_alivesHit.Contains(npc));
            }
            
            return findTargetsByCrosshairDistance
                ? FindNpcUtil.ClosestNpc(npcs, targetEnemies, targetNeutrals, targetAllies, searchRay, owner).FirstOrDefault()
                : FindNpcUtil.NearestNpc(npcs, targetEnemies, targetNeutrals, targetAllies, searchRay.origin, owner).FirstOrDefault();
        }

        protected override void OnGameObjectDestroy() {
            base.OnGameObjectDestroy();
            ResetTarget();
        }

        protected enum TargetUpdateMethod : byte {
            None,
            TrySet,
            TryReplace,
            ReplaceOrClear,
        }
    }
}