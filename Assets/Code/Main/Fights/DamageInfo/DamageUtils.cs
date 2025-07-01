using System;
using System.Collections.Generic;
using Awaken.TG.Main.AI.Fights.Projectiles;
using Awaken.TG.Main.Character;
using Awaken.TG.Main.Executions;
using Awaken.TG.Main.Fights.Factions;
using Awaken.TG.Main.Heroes;
using Awaken.TG.Main.Heroes.Combat;
using Awaken.TG.Main.Heroes.Items;
using Awaken.TG.Main.Heroes.Statuses;
using Awaken.TG.Main.Skills;
using Awaken.TG.MVC;
using Awaken.TG.Utility;
using Awaken.Utility.PhysicUtils;
using JetBrains.Annotations;
using UnityEngine;

namespace Awaken.TG.Main.Fights.DamageInfo {
    public static class DamageUtils {
        public const float SphereDamageTickDuration = 0.05f;
        static readonly List<Action> DamageActions = new();
        static readonly HashSet<HealthElement> AlreadyDamagedElements = new();
        
        public static bool TryDoDamage(IAlive aliveHit, Collider colliderHit, RawDamageData rawDamageData,
            ICharacter attacker,
            ref DamageParameters parameters, Item item = null, Projectile projectile = null,
            StatusDamageType statusDamageType = StatusDamageType.Default, float? overridenRandomnessModifier = null) {
            if (aliveHit is not { HasBeenDiscarded: false, IsAlive: true } && colliderHit == null) {
                return false;
            }

            parameters.DamageTypeData ??= new RuntimeDamageTypeData(DamageType.PhysicalHitSource);

            Vector3? position = parameters.Position;
            Vector3? direction = parameters.Direction;
            Vector3? forceDirection = parameters.ForceDirection;

            IAlive target = null;
            HealthElement healthElement = null;
            if (colliderHit != null) {
                Damage.DetermineTargetHit(colliderHit, out target, out healthElement);
                CalculatePosAndDir(colliderHit.transform.position);
            } else if (aliveHit != null) {
                target = aliveHit;
                healthElement = aliveHit.HealthElement;
                CalculatePosAndDir(aliveHit.Coords);
            }

            if (healthElement != null) {
                parameters.Position = position;
                parameters.Direction = direction;
                parameters.ForceDirection = forceDirection;

                if (!parameters.DealerPosition.HasValue && attacker != null) {
                    parameters.DealerPosition = attacker is Hero h ? h.CoordsOnNavMesh : attacker.Coords;
                }

                Damage damage = new Damage(parameters, attacker, target, rawDamageData).WithHitCollider(colliderHit)
                    .WithItem(item).WithProjectile(projectile).WithStatusDamageType(statusDamageType)
                    .WithOverridenRandomOccurrenceEfficiency(overridenRandomnessModifier ?? item?.ItemStats?.RandomOccurrenceEfficiency ?? 1f);
                healthElement.TakeDamage(damage);
                return true;
            }

            return false;
            
            void CalculatePosAndDir(Vector3 hitPos) {
                position ??= hitPos;
                direction ??= attacker != null ? (position.Value - attacker.Coords).normalized.ToHorizontal3() : Vector3.zero;
                forceDirection ??= direction;
            }
        }

        public static void DealDamageInSphereInstantaneous([CanBeNull] IModel attacker, SphereDamageParameters sphereDamageParameters, Vector3 origin) {
            AlreadyDamagedElements.Clear();
            DealDamageInSphereInternal(attacker, sphereDamageParameters, origin, sphereDamageParameters.endRadius, in AlreadyDamagedElements);
        }
        
        public static void DealDamageInSphereOverTime([CanBeNull] IModel attacker, SphereDamageParameters sphereDamageParameters, Vector3 origin, float radius, in HashSet<HealthElement> alreadyDamagedElements) {
            DealDamageInSphereInternal(attacker, sphereDamageParameters, origin, radius, alreadyDamagedElements);
        }
        
        static void DealDamageInSphereInternal(IModel attacker, SphereDamageParameters sphereDamageParameters, Vector3 origin, float radius, in HashSet<HealthElement> alreadyDamagedElements) {
            DamageActions.Clear();
            var query = PhysicsQueries.OverlapSphere(origin, radius, sphereDamageParameters.hitMask, QueryTriggerInteraction.Collide);
            var attackerHealthElement = attacker is IAlive alive ? alive.HealthElement : attacker?.TryGetElement<IAlive>()?.HealthElement;
            foreach (var collider in query) {
                DealDamageInstanceInAreaInternal(collider, attacker, attackerHealthElement, sphereDamageParameters, origin, radius, alreadyDamagedElements);
            }
            World.Services.Get<MitigatedExecution>().RegisterOverTime(DamageActions, SphereDamageTickDuration, attacker, MitigatedExecution.Cost.Heavy, MitigatedExecution.Priority.High, 0.1f);
        }
        
        public static void DealDamageInConeInstantaneous([CanBeNull] IModel attacker, ConeDamageParameters coneDamageParameters, Vector3 origin) {
            AlreadyDamagedElements.Clear();
            DealDamageInConeInternal(attacker, coneDamageParameters, origin, coneDamageParameters.sphereDamageParameters.endRadius, AlreadyDamagedElements);
        }
        
        public static void DealDamageInConeOverTime([CanBeNull] IModel attacker, ConeDamageParameters coneDamageParameters, Vector3 origin, float radius, in HashSet<HealthElement> alreadyDamagedElements) {
            DealDamageInConeInternal(attacker, coneDamageParameters, origin, radius, alreadyDamagedElements);
        }
        
        static void DealDamageInConeInternal(IModel attacker, ConeDamageParameters coneDamageParameters, Vector3 origin, float radius, in HashSet<HealthElement> alreadyDamagedElements) {
            DamageActions.Clear();
            var query = PhysicsQueries.OverlapConeApprox(origin, radius, coneDamageParameters.angle, coneDamageParameters.forward, coneDamageParameters.sphereDamageParameters.hitMask, QueryTriggerInteraction.Collide);
            var attackerHealthElement = attacker is IAlive alive ? alive.HealthElement : attacker?.TryGetElement<IAlive>()?.HealthElement;
            foreach (var collider in query) {
                DealDamageInstanceInAreaInternal(collider, attacker, attackerHealthElement, coneDamageParameters.sphereDamageParameters, origin, radius, alreadyDamagedElements);
            }
            World.Services.Get<MitigatedExecution>().RegisterOverTime(DamageActions, SphereDamageTickDuration, attacker, MitigatedExecution.Cost.Heavy, MitigatedExecution.Priority.High, 0.1f);
        }

        static void DealDamageInstanceInAreaInternal(Collider colliderHit, IModel attacker, HealthElement attackerHealthElement, SphereDamageParameters sphereDamageParameters, Vector3 origin, float radius, in HashSet<HealthElement> alreadyDamagedElements) {
            Damage.DetermineTargetHit(colliderHit, out IAlive receiver, out HealthElement healthElement);
            if (receiver != null && healthElement != null && healthElement != attackerHealthElement && alreadyDamagedElements.Add(healthElement)) {
                if (sphereDamageParameters.disableFriendlyFire && receiver is ICharacter receiverCharacter && attacker is ICharacter attackerCharacter && receiverCharacter.IsFriendlyTo(attackerCharacter)) {
                    return;
                }
                DamageParameters parameters = sphereDamageParameters.baseDamageParameters;
                parameters.Position = colliderHit.bounds.center;
                Vector3 direction = (colliderHit.bounds.center - origin).normalized;
                parameters.Direction = direction;
                parameters.ForceDirection = parameters.Direction;
                float distanceFactor = Mathf.Clamp(direction.magnitude / radius, 0, 1);
                if (distanceFactor >= 0.5f) {
                    distanceFactor = distanceFactor.RemapTo01(0.5f, 1, true);
                    parameters.ForceDamage = Mathf.Lerp(parameters.ForceDamage, parameters.ForceDamage * 0.25f, distanceFactor);
                    parameters.RagdollForce = Mathf.Lerp(parameters.RagdollForce, parameters.RagdollForce * 0.25f, distanceFactor);
                }
                if (receiver is ICharacter character && sphereDamageParameters.onHitStatusTemplate is { IsSet: true }) {
                    var template = sphereDamageParameters.onHitStatusTemplate.Get<StatusTemplate>();
                    StatusSourceInfo statusSourceInfo = StatusSourceInfo.FromStatus(template);
                    if (attacker is ICharacter attackerChar) {
                        statusSourceInfo = statusSourceInfo.WithCharacter(attackerChar);
                    }
                    if (template.IsBuildupAble) {
                        character.Statuses?.BuildupStatus(sphereDamageParameters.onHitStatusBuildup, template, statusSourceInfo);
                    } else {
                        character.Statuses?.AddStatus(template, statusSourceInfo);
                    }
                }
                Damage damage = new Damage(parameters, attacker as ICharacter, receiver, new RawDamageData(sphereDamageParameters.rawDamageData.CalculatedValue))
                    .WithHitCollider(colliderHit).WithItem(sphereDamageParameters.item)
                    .WithOverridenRandomOccurrenceEfficiency(sphereDamageParameters.overridenRandomnessModifier ?? sphereDamageParameters.item?.ItemStats?.RandomOccurrenceEfficiency ?? 1f);
                DamageActions.Add(() => healthElement.TakeDamage(damage));
            }
        }

        public static DamageSubType DefaultSubtype(this DamageType damageType) {
            return damageType switch {
                DamageType.None => DamageSubType.GenericPhysical,
                DamageType.PhysicalHitSource => DamageSubType.GenericPhysical,
                DamageType.MagicalHitSource => DamageSubType.GenericMagical,
                DamageType.Status => DamageSubType.Pure,
                DamageType.Fall => DamageSubType.Pure,
                DamageType.Interact => DamageSubType.Pure,
                DamageType.Environment => DamageSubType.GenericPhysical,
                DamageType.Trap => DamageSubType.GenericPhysical,
                _ => throw new ArgumentOutOfRangeException(nameof(damageType), damageType, null)
            };
        }
        
        public static bool IsSpecial(this DamageSubType subType) {
            switch (subType) {
                case DamageSubType.Pure:
                case DamageSubType.Wyrdness:
                    return true;
                default:
                    return false;
            }
        }
        
        public static bool IsPhysical(this DamageSubType subType) {
            switch (subType) {
                case DamageSubType.GenericPhysical:
                case DamageSubType.Slashing:
                case DamageSubType.Piercing:
                case DamageSubType.Bludgeoning:
                    return true;
                default:
                    return false;
            }
        }
        
        public static bool IsMagical(this DamageSubType subType) {
            switch (subType) {
                case DamageSubType.GenericMagical:
                case DamageSubType.Fire:
                case DamageSubType.Cold:
                case DamageSubType.Poison:
                case DamageSubType.Electric:
                    return true;
                default:
                    return false;
            }
        }

        [UnityEngine.Scripting.Preserve]
        public static DamageSubType[] AllPhysicalSubtypes => new[] {
            DamageSubType.GenericPhysical,
            DamageSubType.Slashing,
            DamageSubType.Piercing,
            DamageSubType.Bludgeoning,
        };
        
        [UnityEngine.Scripting.Preserve]
        public static DamageSubType[] AllMagicalSubtypes => new[] {
            DamageSubType.GenericMagical,
            DamageSubType.Fire,
            DamageSubType.Cold,
            DamageSubType.Poison,
            DamageSubType.Electric,
        };
    }
}
