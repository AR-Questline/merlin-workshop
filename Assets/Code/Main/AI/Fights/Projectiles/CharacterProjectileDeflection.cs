using System.Collections.Generic;
using Awaken.TG.Main.AI.Fights.Archers;
using Awaken.TG.Main.AI.Grid;
using Awaken.TG.Main.Character;
using Awaken.TG.Main.Fights.DamageInfo;
using Awaken.TG.Main.Fights.Factions;
using Awaken.TG.Main.General.StatTypes;
using Awaken.TG.Main.Grounds;
using Awaken.TG.Main.Heroes;
using Awaken.TG.Main.Heroes.Combat;
using Awaken.TG.MVC;
using Awaken.TG.MVC.Elements;
using Awaken.TG.MVC.Events;
using JetBrains.Annotations;
using Unity.Collections;
using Unity.Mathematics;
using UnityEngine;
using Object = UnityEngine.Object;
using Random = UnityEngine.Random;

namespace Awaken.TG.Main.AI.Fights.Projectiles {
    public partial class CharacterProjectileDeflection : Element<ICharacter> {
        public sealed override bool IsNotSaved => true;

        NpcGrid _npcGrid;

        const float TargetHeightOffsetMultiplier = 0.65f;
        const float TargetMaxDistance = 20f;
        const float TargetEnemyRangeAngle = 5f;
        const float VelocityMultiplier = 1.1f;
        const float LowestPrecisionSpreadAngle = 70f;
        const float HighestPrecisionSpreadAngle = 5f;
        const float MinDeflectionVelocity = 5f;
        
        const float TargetEnemyRangeAngleRadians = TargetEnemyRangeAngle * Mathf.Deg2Rad; 
        
        public bool DeflectionEnabled { get; private set; }
        public bool DeflectionRandomDirection { get; private set; } = true;
        public List<DamageType> DeflectionTypes { get; } = new() {DamageType.PhysicalHitSource};
        
        public new static class Events {
            public static readonly Event<ICharacter, Damage> CharacterDeflectedProjectile = new(nameof(CharacterDeflectedProjectile));
        }
        
        public static CharacterProjectileDeflection GetOrCreate(ICharacter character) {
            if (character == null) {
                return null;
            }
            
            return character.TryGetElement<CharacterProjectileDeflection>() ?? character.AddElement(new CharacterProjectileDeflection());
        }

        public void EnableDeflection() => DeflectionEnabled = true;
        public void DisableDeflection() => DeflectionEnabled = false;

        public void SetDeflectionDirectionRandom() => DeflectionRandomDirection = true;
        public void SetDeflectionTargetEnemy() => DeflectionRandomDirection = false;

        public void EnableDeflectionForType(DamageType type) {
            if (!DeflectionTypes.Contains(type)) {
                DeflectionTypes.Add(type);
            }
        }

        public void DisableDeflectionForType(DamageType type) => DeflectionTypes.RemoveSwapBack(type);

        public void SetDeflectionTypes(DamageType[] types) {
            DeflectionTypes.Clear();
            DeflectionTypes.AddRange(types);
        }

        protected override void OnInitialize() {
            ParentModel.HealthElement.ListenTo(HealthElement.Events.TakingDamage, OnTakingDamage, this);
        }
        
        // === Public API
        [UsedImplicitly, UnityEngine.Scripting.Preserve]
        public void TryDeflectProjectile(Damage damage, DeflectionBehaviour behaviour) {
            if (damage.Projectile != null) {
                DeflectProjectile(damage, damage.Projectile, behaviour);
            }
        }

        public void DeflectProjectile(Damage damage, Projectile projectile, bool randomDirection) {
            var deflectionBehaviour = randomDirection
                ? DeflectionBehaviour.Random
                : DeflectionBehaviour.TargetEnemy;
            DeflectProjectile(damage, projectile, deflectionBehaviour);
        }
        
        public void DeflectProjectile(Damage damage, Projectile projectile, DeflectionBehaviour behaviour) {
            Vector3 shotStartPosition = projectile.transform.position;
            float precision = ParentModel.Stat(CharacterStatType.DeflectPrecision)?.ModifiedValue ?? 0.0f;
            
            Vector3 targetPosition = behaviour switch {
                DeflectionBehaviour.Random => GetDeflectionForwardRandomizedPosition(shotStartPosition, precision),
                DeflectionBehaviour.TargetEnemy => GetDeflectionEnemySnappedPosition(shotStartPosition, precision),
                DeflectionBehaviour.Deflect => GetDeflectionPosition(damage),
                _ => GetDeflectionForwardRandomizedPosition(shotStartPosition, precision)
            };
            
            FireProjectile(damage, shotStartPosition, targetPosition, projectile);
        }

        // === Event Handlers
        void OnTakingDamage(HookResult<HealthElement, Damage> hook) {
            if (!CanDeflectProjectile(hook.Value, out var projectile)) {
                return;
            }
            hook.Prevent();
            DeflectProjectile(hook.Value, projectile, DeflectionRandomDirection);
        }

        // === Helpers
        bool CanDeflectProjectile(Damage damage, out Projectile projectile) {
            // --- Without info about projectile we cannot deflect it.
            projectile = damage.Projectile;
            if (projectile == null) {
                return false;
            }

            if (!DeflectionEnabled) {
                return false;
            }

            // --- Check whether we can deflect the projectile type
            if (!DeflectionTypes.Contains(damage.Parameters.Type)) {
                return false;
            }

            return true;
        } 

        Vector3 GetDeflectionForwardRandomizedPosition(Vector3 shotStartPosition, float precision) {
            // Random position in frontal angle from the character
            float spreadAngle = Mathf.Lerp(LowestPrecisionSpreadAngle, HighestPrecisionSpreadAngle, precision);
            Quaternion randomRotation = Quaternion.Euler(0, spreadAngle * Random.Range(-1f, 1f), 0);
            return shotStartPosition + randomRotation * GetDeflectionForwardDirection() * 10f;
        }

        Vector3 GetDeflectionForwardDirection() {
            if (ParentModel is Hero hero) {
                return hero.VHeroController.LookDirection;
            }

            return ParentModel.Forward();
        }
        
        Vector3 GetDeflectionEnemySnappedPosition(Vector3 shotStartPosition, float precision) {
            var target = FindClosestPotentialTarget();
            var randomPosition = GetDeflectionForwardRandomizedPosition(shotStartPosition, precision);

            if (target == null) {
                return randomPosition;
            }
            
            var enemyTargetPosition = target.Coords + Vector3.up * (target.Height * TargetHeightOffsetMultiplier);
            return Vector3.Lerp(randomPosition, enemyTargetPosition, precision);
        }

        ICharacter FindClosestPotentialTarget() {
            float closestDistanceSqr = float.MaxValue;
            ICharacter target = null;
            Vector3 parentCoords = ParentModel.Coords;
            foreach (var potentialTarget in GetPotentialTargets()) {
                if (potentialTarget.AntagonismTo(ParentModel) != Antagonism.Hostile) {
                    continue;
                }
                
                float distanceSqr = (parentCoords - potentialTarget.Coords).sqrMagnitude;
                if (distanceSqr < closestDistanceSqr) {
                    closestDistanceSqr = distanceSqr;
                    target = potentialTarget;
                }
            }

            return target;
        }

        IEnumerable<ICharacter> GetPotentialTargets() {
            _npcGrid ??= World.Services.Get<NpcGrid>();
            var closestNPCs = _npcGrid.GetNpcsInCone(
                ParentModel.Coords, 
                GetDeflectionForwardDirection(), 
                TargetMaxDistance, 
                TargetEnemyRangeAngleRadians);

            foreach (var npc in closestNPCs) {
                yield return npc;
            }
            
            if (ParentModel is not Hero) {
                yield return Hero.Current;
            }
        }
        
        Vector3 GetDeflectionPosition(Damage damage) {
            ICharacter target = ParentModel;
            Vector3 targetCoords = target.Head != null 
                ? target.Head.position 
                : target.Coords + Vector3.up * target.Height;
            
            Vector3 damagePosition = damage.Position ?? damage.Projectile.transform.position;
            Vector3 projectileDirection = damage.Projectile.Velocity.normalized;
            Vector3 damageToTargetDirection = (targetCoords - damagePosition).normalized;
            
            bool hitLeftOfTarget = Vector3.SignedAngle(damageToTargetDirection, projectileDirection, Vector3.up) > 0;
            
            // Random deflection angle behind the target
            float angle = Random.Range(90f, 120f) * (hitLeftOfTarget ? -1 : 1);
            
            Vector3 randomDirection = Quaternion.Euler(0, angle, 0) * -projectileDirection;

#if false
            var color = RandomColor;
            // marker for damage position
            var marker = GameObject.CreatePrimitive(PrimitiveType.Cube);
            marker.transform.position = damagePosition;
            SetupMarker(marker, color, damageToTargetDirection);
            
            //marker for projectile direction
            marker = GameObject.CreatePrimitive(PrimitiveType.Cube);
            marker.transform.position = damagePosition;
            SetupMarker(marker, color, projectileDirection);
            
            //marker for random direction
            marker = GameObject.CreatePrimitive(PrimitiveType.Cube);
            marker.transform.position = damagePosition + randomDirection;
            SetupMarker(marker, color, randomDirection);
#endif
            
            return targetCoords + randomDirection * 10f;
        }

        void FireProjectile(Damage damage, Vector3 shotPosition, Vector3 targetPosition, Projectile projectile) {
            float speed = math.max(MinDeflectionVelocity, projectile.Velocity.magnitude) * VelocityMultiplier;

            Vector3 velocity;
            
            bool useParabolicShot = projectile.UsesGravity;
            if (useParabolicShot) {
                velocity = ArcherUtils.ShotVelocity(new ShotData(shotPosition, targetPosition, speed, false));
            } else {
                velocity = (targetPosition - shotPosition).normalized * speed;
            }
            FireProjectile(damage, velocity, projectile);
        }
        
        void FireProjectile(Damage damage, Vector3 velocity, Projectile projectile) {
            DeflectedProjectileParameters parameters = new(ParentModel, velocity);
            
            // --- Trigger deflection
            projectile.DeflectProjectile(parameters);
            ParentModel.Trigger(Events.CharacterDeflectedProjectile, damage);
        }
        
        // === Debug
        [UnityEngine.Scripting.Preserve] static Color RandomColor => new Color(Random.value, Random.value, Random.value);
        
        [UnityEngine.Scripting.Preserve]
        static void SetupMarker(GameObject marker, Color color, Vector3 forward) {
            marker.transform.localScale = new Vector3(0.05f, 0.05f, 0.3f);
            marker.transform.forward = forward;
            //shift marker to the front
            marker.transform.position += forward * 0.15f;
            Object.Destroy(marker.GetComponent<Collider>());
            marker.GetComponent<MeshRenderer>().material.color = color;
            Object.Destroy(marker, 2f);
        }
    }
    
    public enum DeflectionBehaviour : byte {
        [UnityEngine.Scripting.Preserve] None,
        Random,
        TargetEnemy,
        Deflect
    }
}
