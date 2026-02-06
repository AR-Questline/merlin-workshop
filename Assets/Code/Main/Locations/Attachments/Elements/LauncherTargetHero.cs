using Awaken.CommonInterfaces;
using Awaken.TG.Main.AI.Fights.Archers;
using Awaken.TG.Main.Character;
using Awaken.TG.Main.Fights;
using Awaken.TG.Main.Fights.DamageInfo;
using Awaken.TG.Main.Fights.NPCs;
using Awaken.TG.Main.Heroes;
using Awaken.TG.Main.Locations.Attachments.Attachment;
using Awaken.TG.Main.Locations.Attachments.Elements.DeathBehaviours;
using Awaken.TG.Main.Settings.Gameplay;
using Awaken.TG.Main.Timing.ARTime;
using Awaken.TG.Main.Utility.Animations.ARAnimator;
using Awaken.TG.Main.Utility.Debugging;
using Awaken.TG.MVC;
using Awaken.TG.MVC.Elements;
using Awaken.TG.MVC.Utils;
using Awaken.Utility;
using Awaken.Utility.Animations;
using Awaken.Utility.Debugging;
using Awaken.Utility.GameObjects;
using Awaken.Utility.Maths;
using JetBrains.Annotations;
using UnityEngine;

namespace Awaken.TG.Main.Locations.Attachments.Elements {
    public partial class LauncherTargetHero : Element<Location>, IRefreshedByAttachment<LauncherTargetHeroAttachment>, UnityUpdateProvider.IWithUpdateGeneric {
        public override ushort TypeForSerialization => SavedModels.LauncherTargetHero;
        
        LauncherTargetHeroAttachment _spec;
        WeakModelRef<IAlive> _spawnedOperator;
        InteractionTriggerSkills _skillsTrigger;
        float _minDistanceToTrackSqr;
        float _maxDistanceToAttackSqr;
        float _operatorRespawnTime = -1f;
        Transform _ballistaHeadTransform;
        Vector3 _firePointLocalPosition;
        Quaternion _ballistaBaseRotation;
        Quaternion _currentRotation;
        
        float _launchTime = -1;
        Vector3 _launchTargetOffeset;
        DifficultySetting _difficultySetting;
        LauncherTargetHeroAttachment.DifficultyParameters _currentParams;
        
        [NotNull]
        DifficultySetting DifficultySetting {
            get {
                if (_difficultySetting == null || _difficultySetting.HasBeenDiscarded) {
                    _difficultySetting = World.Only<DifficultySetting>();
                }
                return _difficultySetting;
            }
        }

        LauncherTargetHeroAttachment.DifficultyParameters CurrentParams {
            get {
                var currentDifficulty = DifficultySetting.Difficulty;
                return _spec.GetParametersForDifficulty(currentDifficulty);
            }
        }

        public void InitFromAttachment(LauncherTargetHeroAttachment spec, bool isRestored) {
            _spec = spec;
            _minDistanceToTrackSqr = spec.minDistanceToTrack * spec.minDistanceToTrack;
            _maxDistanceToAttackSqr = spec.maxDistanceToAttack * spec.maxDistanceToAttack;
        }

        protected override void OnFullyInitialized() {
            _skillsTrigger = ParentModel.Element<InteractionTriggerSkills>();
            
            ParentModel.OnVisualLoaded(transform => {
                _ballistaBaseRotation = transform.rotation;
                _ballistaHeadTransform = transform.gameObject.FindChildWithTagRecursively(_spec.ballistaHeadTag);
                if (_ballistaHeadTransform == null) {
                    Log.Important?.Error($"Ballista head with tag '{_spec.ballistaHeadTag}' not found in hierarchy of {ParentModel}.");
                    return;
                }

                var firePointTransform = _ballistaHeadTransform.Find("FirePoint");
                if (firePointTransform == null) {
                    Log.Important?.Error($"FirePoint not found as child of ballista head in {ParentModel}. Using ballista head position as fallback.");
                    _firePointLocalPosition = Vector3.zero;
                } else {
                    _firePointLocalPosition = firePointTransform.localPosition;
                }

                _ballistaHeadTransform.gameObject.SetUnityRepresentation(new IWithUnityRepresentation.Options {
                    linkedLifetime = true,
                    movable = true
                });
                _currentRotation = _ballistaHeadTransform.localRotation;
                UnityUpdateProvider.GetOrCreate().RegisterGeneric(this);
                if (!LocationInCombatCheckFailed(this)) {
                    ScheduleNextLaunch(_spec.randomInitialDelayDecrease);
                }
                SpawnOperatorIfRequired(transform);
            });
        }

        protected override void OnDiscard(bool fromDomainDrop) {
            UnityUpdateProvider.TryGet()?.UnregisterGeneric(this);
            base.OnDiscard(fromDomainDrop);
        }

        void ScheduleNextLaunch(float randomInitialDelayDecrease = 0f) {
            var randomDelta = Random.Range(-_spec.randomIntervalDelta, _spec.randomIntervalDelta);
            var initialDelayReduction = Random.Range(0f, randomInitialDelayDecrease);
            
            var currentParams = CurrentParams;
            _launchTime = Time.time + currentParams.launchInterval + randomDelta - initialDelayReduction;
            
            _launchTargetOffeset = Random.insideUnitSphere.WithY(0.5f) * currentParams.targetRandomRadius;
        }

        void SpawnOperatorIfRequired(Transform ballistaView) {
            if (!_spec.RequireOperatorToFunction || !_spec.spawnOperatorAtStart) {
                return;
            }
            var seatTransform = ballistaView.gameObject.FindChildWithTagRecursively(_spec.operatorSeatTag);
            if (seatTransform == null) {
                Log.Important?.Error($"Operator seat with tag '{_spec.operatorSeatTag}' not found in hierarchy of {LogUtils.GetDebugName(ParentModel)}.");
                return;
            }
            var operatorLocation = _spec.OperatorLocation;
            if (operatorLocation == null) {
                Log.Important?.Error($"Operator location template is null in {LogUtils.GetDebugName(ParentModel)}.");
                return;
            }

            var spawnedLocation = operatorLocation.SpawnLocation(overridenLocationName: $"{ID}_Operator");
            var aliveOperator = spawnedLocation.Element<IAlive>();
            _spawnedOperator = new(aliveOperator);
            
            aliveOperator.ListenTo(IAlive.Events.AfterDeath, damageOutcome => AfterOperatorDeath(spawnedLocation, damageOutcome), this);
            spawnedLocation.OnVisualLoaded(_ => AttachToSeat(spawnedLocation.LocationView.transform, seatTransform));
            _skillsTrigger.LockForHero();
        }
        
        void AfterOperatorDeath(Location spawnedLocation, DamageOutcome damageOutcome) {
            var spawnedLocationView = spawnedLocation.LocationView;
            var ragdollController = spawnedLocationView.GetComponentInChildren<RagdollController>();
            var rootBone = ragdollController.rootBone;
            
            ragdollController.ApplyRagdoll();
            
            var forceData = DeathRagdollBehaviour.SetupFromDamageOutcome(damageOutcome);
            var hitPosition = forceData.hitPosition.GetValueOrDefault(rootBone.position);
            DeathRagdollBehaviour.ApplyForces(forceData, rootBone.GetComponent<Rigidbody>(), spawnedLocation.GetTimeScale(), hitPosition, Optional<Vector3>.None);
            
            spawnedLocationView.GetComponentInChildren<ARNpcAnimancer>().enabled = false;
            _skillsTrigger.UnlockForHero();
        }

        void AttachToSeat(Transform transform, Transform seatTransform) {
            transform.SetParent(seatTransform, worldPositionStays: false);
        }

        public void UnityUpdate() {
            // Look at hero
            if (_ballistaHeadTransform == null) return;
            
            var hero = Hero.Current;
            if (hero == null) return;

            if (LocationInCombatCheckFailed(this)) {
                return;
            }
            
            if (OperatorCheckFailed(this)) {
                if (_spec.returnToBaseRotationWithoutOperator) {
                    ReturnToBaseRotation();
                }
                return;
            }

            if (_launchTime < 0) {
                ScheduleNextLaunch(_spec.randomInitialDelayDecrease);
            }
            
            var currentParams = CurrentParams;
            var targetPosition = hero.Coords + _launchTargetOffeset;
            var firePoint = _ballistaHeadTransform.TransformPoint(_firePointLocalPosition);
            
            // Range check
            var distanceVector = targetPosition - firePoint;
            float distanceSqrMagnitude = distanceVector.sqrMagnitude;
            if (distanceSqrMagnitude <= _minDistanceToTrackSqr || distanceSqrMagnitude >= _maxDistanceToAttackSqr) 
                return;

            Vector3 direction = GetBallisticShotDirection(firePoint, targetPosition);

            // Transform direction into ballista base's local space
            // This accounts for the ballista's base rotation
            var localDirection = Quaternion.Inverse(_ballistaBaseRotation) * direction;
            
            // Extract components for angle calculations
            var localForwardDistance = localDirection.z;
            var localRightDistance = localDirection.x;
            var localVerticalDistance = localDirection.y;
            
            // Calculate horizontal distance (for pitch)
            var localHorizontalDistance = Mathf.Sqrt(localForwardDistance * localForwardDistance + localRightDistance * localRightDistance);
            
            if (localHorizontalDistance < 0.001f) {
                return; // Target is directly above/below, skip rotation
            }

            // Calculate yaw angle in local space (rotation around local Y axis)
            // Yaw is the angle between forward and the horizontal projection of the direction
            bool anyClamping = false;
            float clampedYawAngle = ClampedAngle(localRightDistance, localForwardDistance, _spec.yawLimit, ref anyClamping);

            // Calculate pitch angle in local space (rotation around local X axis)
            // Pitch is the angle between the horizontal plane and the direction
            float clampedPitchAngle = ClampedAngle(localVerticalDistance, localHorizontalDistance, _spec.pitchLimit, ref anyClamping);

            // Calculate local yaw rotation (rotation around local Y axis)
            var localYawRotation = Quaternion.AngleAxis(clampedYawAngle, Vector3.up);
            
            // Calculate local pitch rotation (rotation around local X axis)
            var localPitchRotation = Quaternion.AngleAxis(-clampedPitchAngle, Vector3.right);

            // Combine local yaw and pitch to get target rotation
            var targetRotation = Quaternion.Normalize(localYawRotation * localPitchRotation);
            
            // Smoothly rotate towards target with max rotation speed
            float maxRotationDelta = currentParams.maxRotationSpeed * Time.deltaTime;
            _currentRotation = Quaternion.RotateTowards(_currentRotation, targetRotation, maxRotationDelta);
            _ballistaHeadTransform.localRotation = _currentRotation;
                
            // Launch at hero only if time has elapsed, not clamped, and accurately aimed
            if (!anyClamping && Time.time >= _launchTime) {
                float angleToTarget = Quaternion.Angle(_currentRotation, targetRotation);
                bool isAccuratelyAimed = angleToTarget <= currentParams.firingAccuracyAngle;
                
                if (isAccuratelyAimed) {
                    LaunchAtHero();
                    ScheduleNextLaunch();
                }
            }
        }

        void ReturnToBaseRotation() {
            var currentParams = CurrentParams;
            var targetRotation = Quaternion.identity;
            float maxRotationDelta = currentParams.maxRotationSpeed * Time.deltaTime;
            _currentRotation = Quaternion.RotateTowards(_currentRotation, targetRotation, maxRotationDelta);
            _ballistaHeadTransform.localRotation = _currentRotation;
        }

        static bool LocationInCombatCheckFailed(LauncherTargetHero launcher) {
            if (!launcher._spec.activeOnlyWhenLocationInCombat) {
                return false;
            }

            if (!launcher._spec.locationThatHasToBeInCombat.IsSet) {
                return false;
            }

            foreach (var location in launcher._spec.locationThatHasToBeInCombat.MatchingLocations(null)) {
                if (location.TryGetElement(out NpcElement npc) && npc.IsInCombat()) {
                    return false;
                }
            }

            return true;
        }

        static bool OperatorCheckFailed(LauncherTargetHero launcher) {
            if (!launcher._spec.RequireOperatorToFunction) {
                return false;
            }
            if (!launcher._spawnedOperator.Exists()) {
                if (launcher._spec.operatorRespawnDelay < 0f) {
                    return true;
                }
                if (launcher._operatorRespawnTime  < 0f) {
                    launcher._operatorRespawnTime = Time.time + launcher._spec.operatorRespawnDelay;
                    return true;
                }
                if (launcher._operatorRespawnTime > Time.time) {
                    return true;
                }

                launcher._operatorRespawnTime = -1;
                launcher.SpawnOperatorIfRequired(launcher._ballistaHeadTransform.parent);
                
                return false;
            }
            return !launcher._spawnedOperator.Get().IsAlive;
        }

        float ClampedAngle(float component1, float component2, float limit, ref bool anyClamping) {
            var desiredAngle = Mathf.Atan2(component1, component2) * Mathf.Rad2Deg;
            var clampedAngle = Mathf.Clamp(desiredAngle, -limit, limit);
            if (!anyClamping) {
                anyClamping |= Mathf.Abs(desiredAngle - clampedAngle) > 0.01f;
            }
            return clampedAngle;
        }

        Vector3 GetBallisticShotDirection(Vector3 firePoint, Vector3 targetPosition) {
            var currentParams = CurrentParams;
            var shotData = new ShotData(firePoint, targetPosition, currentParams.projectileVelocity, highShot: false);
            var shotVelocity = ArcherUtils.ShotVelocity(shotData);
            var direction = shotVelocity.normalized;
            return direction;
        }

        void LaunchAtHero() {
            var hero = Hero.Current;
            if (hero == null) {
                return;
            }
            
            // Launch projectile towards targetPosition
            _skillsTrigger.StartInteraction(hero, ParentModel);
        }
    }
}
