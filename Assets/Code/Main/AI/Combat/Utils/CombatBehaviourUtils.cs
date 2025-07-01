using System;
using Awaken.TG.Main.AI.Combat.Attachments;
using Awaken.TG.Main.AI.Combat.Behaviours.Abstracts;
using Awaken.TG.Main.AI.Fights.Archers;
using Awaken.TG.Main.AI.Fights.Projectiles;
using Awaken.TG.Main.AI.Movement;
using Awaken.TG.Main.Character;
using Awaken.TG.Main.Fights.NPCs;
using Awaken.TG.Main.General;
using Awaken.TG.Main.Heroes;
using Awaken.TG.Main.VisualGraphUtils;
using Awaken.TG.Utility;
using Awaken.Utility.Maths;
using UnityEngine;

namespace Awaken.TG.Main.AI.Combat.Utils {
    public static class CombatBehaviourUtils {
        public static readonly FloatRange DefaultFireAngleRange = new(-60f, 60f);
        public const float DefaultEnemyHeight = 1.5f;
        const float MinProjectileVelocity = 10f;

        public static CharacterPlace GetTargetPosition(EnemyBaseClass enemy, ICharacter target, float desiredDistanceToTarget, float positionRadius = 0.1f) {
            if (enemy == null) {
                return new CharacterPlace(Vector3.zero, positionRadius);
            }
            Vector3 myPosition = enemy.Coords;
            Vector3 targetPos = myPosition;

            if (target != null && target.ParentTransform != null) {
                targetPos = target.Coords;
                Vector3 targetForward = target.ParentTransform.forward;
                Vector3 directionFromTarget = myPosition - targetPos;

                if (enemy.NpcElement.Controller.ForwardMovementOnly) {
                    targetPos += directionFromTarget.normalized * desiredDistanceToTarget;
                } else {
                    float angleTowardsTarget = Vector3.SignedAngle(targetForward, directionFromTarget, Vector3.up);
                    if (angleTowardsTarget is > 45f or < -45f) {
                        targetPos += Quaternion.AngleAxis(Mathf.Sign(angleTowardsTarget) * 30, Vector3.up) *
                                     targetForward * desiredDistanceToTarget;
                    } else if (angleTowardsTarget is > 15f or < -15f) {
                        targetPos += directionFromTarget.normalized * desiredDistanceToTarget;
                    } else {
                        targetPos += Quaternion.AngleAxis(Mathf.Sign(angleTowardsTarget) * 15f - angleTowardsTarget, Vector3.up) *
                                     directionFromTarget.normalized *
                                     desiredDistanceToTarget;
                    }
                }
            }

            return new CharacterPlace(targetPos, positionRadius);
        }

        public static ProjectileWrapper FireProjectile(FireProjectileParams fireParams, VGUtils.ShootParams shootParams) {
            bool useShootingPos;
            Vector3 targetPosition;
            if (fireParams.target != null) {
                useShootingPos = false;
                targetPosition = fireParams.target.Coords;
            } else {
                useShootingPos = fireParams.shootPos != Vector3.zero;
                targetPosition = useShootingPos ? fireParams.shootPos : fireParams.shooterView.transform.forward * fireParams.maxVelocity;
            }

            if (fireParams.inaccuracy > 0) {
                targetPosition += UnityEngine.Random.insideUnitSphere * fireParams.inaccuracy;
            }
            
            float enemyHeight;
            if (useShootingPos) {
                enemyHeight = 0;
            } else {
                enemyHeight = fireParams.target?.Height * fireParams.aimAtEnemyHeight ?? DefaultEnemyHeight;
                targetPosition += Vector3.up * enemyHeight;
            }
            
            Vector3 directionToTarget = targetPosition.ToHorizontal3() - shootParams.startPosition.ToHorizontal3();
            float distanceToTarget = directionToTarget.magnitude;
            float velocity = distanceToTarget * fireParams.velocityMultiplier;
            velocity = Mathf.Clamp(velocity, MinProjectileVelocity, fireParams.maxVelocity);

            // --- Target movement prediction
            bool predictPlayerMovement = fireParams is { predictPlayerMovement: true};
            if (predictPlayerMovement) {
                targetPosition += GetPredictedMovementPositionOffsetToHero(distanceToTarget / velocity);
            }

            // --- Setup Arrow Velocity
            Vector3 arrowVelocity;
            if (fireParams.parabolicShot) {
                arrowVelocity = ArcherUtils.ShotVelocity(new ShotData(shootParams.startPosition, targetPosition, velocity, fireParams.highShot));
            } else {
                Vector3 projectileVelocity = predictPlayerMovement
                    ? (targetPosition - shootParams.startPosition).normalized * velocity
                    : directionToTarget.normalized * velocity;
                arrowVelocity = projectileVelocity;
            }
            
            // --- Clamp velocity to an angle range
            Vector3 shooterForward = fireParams.shooterView is { Character: NpcElement npc }
                ? npc.Controller.LogicalForward.ToHorizontal3()
                : fireParams.shooterView.transform.forward;
            
            float shootingAngle = Vector3.SignedAngle(shooterForward, arrowVelocity.ToHorizontal3(), Vector3.up);
            float clampedAngle = fireParams.fireAngleRange.Clamp(shootingAngle);
            Vector3 clampedArrowVelocity = Quaternion.AngleAxis(clampedAngle - shootingAngle, Vector3.up) * arrowVelocity;
            
            ProjectileWrapper projectile = VGUtils.ShootProjectile(shootParams, clampedArrowVelocity);
            projectile.HomingProjectileSetTarget(fireParams.target, enemyHeight);

            return projectile;
        }
        
        [UnityEngine.Scripting.Preserve]
        public static Vector3 GetPredictedMovementPositionOffset(ICharacter target, float distanceToTarget, float projectileVelocity) {
            return GetPredictedMovementPositionOffset(target, distanceToTarget / projectileVelocity);
        }
        
        public static Vector3 GetPredictedMovementPositionOffset(ICharacter target, float positionAfterSeconds) {
            return target switch {
                Hero => GetPredictedMovementPositionOffsetToHero(positionAfterSeconds),
                NpcElement npc => GetPredictedMovementPositionOffsetToNpc(npc, positionAfterSeconds),
                _ => Vector3.zero
            };
        }

        public static Vector3 GetPredictedMovementPositionOffsetToHero(float positionAfterSeconds) {
            Vector3 heroVelocity = Hero.Current.VHeroController.HorizontalVelocity;
            return GetPredictedMovementPositionOffset(heroVelocity, positionAfterSeconds);
        }

        public static Vector3 GetPredictedMovementPositionOffsetToNpc(NpcElement npc, float positionAfterSeconds) { 
            var npcVelocity = npc.Movement.Controller.CurrentVelocity.X0Y();
            return GetPredictedMovementPositionOffset(npcVelocity, positionAfterSeconds);
        }

        [UnityEngine.Scripting.Preserve]
        public static Vector3 GetPredictedMovementPositionOffset(Vector3 targetVelocity, float distanceToTarget, float projectileVelocity) {
            return GetPredictedMovementPositionOffset(targetVelocity, distanceToTarget / projectileVelocity);
        }
        
        public static Vector3 GetPredictedMovementPositionOffset(Vector3 targetVelocity, float positionAfterSeconds) {
            return targetVelocity * positionAfterSeconds;
        }

        public static bool TryGetBehaviour(this NpcElement npc, out EnemyBaseClass enemy, out IBehaviourBase behaviour) {
            if (!npc.ParentModel.TryGetElement(out enemy)) {
                throw new Exception("Npc location must have EnemyBaseClass element!");
            }
            return enemy.CurrentBehaviour.TryGet(out behaviour);
        }
        
        [UnityEngine.Scripting.Preserve]
        public static bool IsCurrentlyPeaceful(ICharacter character) {
            if (character is not NpcElement npc) {
                return true;
            }

            if (!npc.CanTriggerAggroMusic) {
                return true;
            }

            return !npc.TryGetBehaviour(out _, out var behaviour) || behaviour.IsPeaceful;
        }

        public struct FireProjectileParams {
            public ICharacterView shooterView;
            public ICharacter target;
            public Vector3 shootPos;
            public FloatRange fireAngleRange;
            public float aimAtEnemyHeight;
            public float maxVelocity;
            public float velocityMultiplier;
            public bool predictPlayerMovement;
            public bool parabolicShot;
            public bool highShot;
            public float inaccuracy;
        }
    }
}