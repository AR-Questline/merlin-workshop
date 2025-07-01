using System.Collections.Generic;
using Awaken.TG.Main.AI;
using Awaken.TG.Main.AI.Combat.Attachments;
using Awaken.TG.Main.AI.Combat.Behaviours.Abstracts;
using Awaken.TG.Main.AI.SummonsAndAllies;
using Awaken.TG.Main.Animations.FSM.Heroes.Machines;
using Awaken.TG.Main.Character;
using Awaken.TG.Main.Fights;
using Awaken.TG.Main.Fights.DamageInfo;
using Awaken.TG.Main.Fights.Factions;
using Awaken.TG.Main.Fights.Factions.Markers;
using Awaken.TG.Main.Fights.NPCs;
using Awaken.TG.Main.Heroes.Items;
using Awaken.TG.Main.Locations.Actions;
using Awaken.TG.Main.Locations.Views;
using Awaken.TG.Main.Saving;
using Awaken.TG.MVC;
using Awaken.TG.MVC.Elements;
using Awaken.Utility.Collections;
using UnityEngine;
using Tool = Awaken.TG.Main.Heroes.Items.Attachments.Tool;

namespace Awaken.TG.Main.Heroes.Combat {
    public partial class CharacterDealingDamage : Element<ICharacter> {
        public const float SoundRange = 8;

        public sealed override bool IsNotSaved => true;
        
        // === Fields
        readonly OnDemandCache<Item, List<HealthElement>> _damageDealtTo = new(_ => new List<HealthElement>());
        // === Properties
        public bool AnyTargetHit => _damageDealtTo.Count > 0;
        HealthElement HealthElement => ParentModel.HealthElement;
        [UnityEngine.Scripting.Preserve] Transform Transform => ParentModel.CharacterView.transform;

        public void OnAttackBegun() {
            _damageDealtTo.Clear();
        }
        
        public void OnWeaponTriggerEnter(RaycastHit hit, in AttackParameters attackParameters, bool inEnviroHitRange, bool calculateHitPoint) {
            bool success = BaseOnTriggerEnter(hit, attackParameters, inEnviroHitRange, calculateHitPoint, out var colliderHit, out var item, out var location, out var direction, out var healthElement, out var receiver);
            if (!success) {
                return;
            }
            
            if (receiver != null && healthElement != HealthElement && !AlreadyDealtDamageWithItem(item, healthElement)) {
                ref readonly var hitbox = ref healthElement.GetHitbox(colliderHit, out var hitboxExists);

                if (hitboxExists && !hitbox.CanBeHit(item)) {
                    return;
                }
                
                DamageParameters parameters = DamageParameters.Default;
                parameters.Position = hit.point;
                parameters.Direction = direction;
                parameters.ForceDirection = direction.normalized;
                parameters.ForceDamage = attackParameters.ForceDamage;
                parameters.RagdollForce = attackParameters.RagdollForce;
                parameters.PoiseDamage = attackParameters.PoiseDamage;
                parameters.IsHeavyAttack = attackParameters.IsHeavyAttack;
                parameters.IsDashAttack = attackParameters.IsDashAttack;
                parameters.IsPush = attackParameters.IsPush;
                
                if (TryGetAttackerCurrentCombatBehaviour(ParentModel, out var combatBehaviour)) {
                    parameters.KnockdownType = combatBehaviour.KnockdownType;
                    parameters.KnockdownStrength = combatBehaviour.KnockdownStrength;
                } else {
                    parameters.KnockdownType = KnockdownType.None;
                    parameters.KnockdownStrength = 0;
                }

                Damage damage = GetDamageToDeal(colliderHit, item, parameters, receiver, location);
                
                if (hitboxExists && hitbox.CanPreventDamage(item)) {
                    healthElement.Trigger(HealthElement.Events.DamagePreventedByHitbox, damage);
                    EnvironmentHit(calculateHitPoint, colliderHit, hit, location, item, direction, attackParameters.RagdollForce);
                    return;
                }
                
                if (damage != null) {
                    MakeHitNoise(ParentModel, receiver);
                    healthElement.TakeDamage(damage);
                    AddDamageDealtTo(item, healthElement);
                    ParentModel?.Trigger(ICharacter.Events.HitAliveWithMeleeWeapon, attackParameters.IsHeavyAttack);
                }
            }
        }
        
        public void OnMagicGauntletTriggerEnter(RaycastHit hit, in AttackParameters attackParameters, bool inEnviroHitRange, bool calculateHitPoint) {
            bool success = BaseOnTriggerEnter(hit, attackParameters, inEnviroHitRange, calculateHitPoint, out var colliderHit, out var item, out var location, out var direction, out var healthElement, out var receiver);
            if (!success) {
                return;
            }
            
            // --- Real Target Hit
            if (AlreadyDealtDamageWithItem(item, healthElement)) {
                return;
            }
            AddDamageDealtTo(item, healthElement);

            var eventData = new MagicGauntletData(ParentModel, receiver, item, attackParameters);
            ParentModel?.Trigger(ICharacter.Events.OnMagicGauntletHit, eventData);
        }

        bool BaseOnTriggerEnter(RaycastHit hit, in AttackParameters attackParameters, bool inEnviroHitRange, bool calculateHitPoint, 
            out Collider colliderHit, out Item item, out VLocation location, out Vector3 direction, out HealthElement returnHealthElement, out IAlive returnReceiver) {
            colliderHit = hit.collider;
            item = attackParameters.Item;
            direction = attackParameters.AttackDirection;
            location = Damage.DetermineTargetHit(colliderHit, out IAlive receiver, out HealthElement healthElement);
            returnHealthElement = healthElement;
            returnReceiver = receiver;
            ICharacter hostileCheck = ParentModel.HasElement<NpcHeroSummon>() ? Hero.Current : ParentModel;
            if (!ParentModel.CanDealDamageToFriendlies && ParentModel is NpcElement npc && receiver is ICharacter character &&
                (!hostileCheck.IsHostileTo(character) || !npc.IsTargetedOrIsTargeting(character))) {
                return false;
            }
            
            // --- Environment Hit
            if (healthElement == null) {
                if (inEnviroHitRange) {
                    EnvironmentHit(calculateHitPoint, colliderHit, hit, location, item, direction, attackParameters.RagdollForce);
                }
                return false;
            }

            return true;
        }

        void AddDamageDealtTo(Item item, HealthElement healthElement) {
            _damageDealtTo[item].Add(healthElement);
        }

        static void MakeHitNoise(ICharacter attacker, IAlive receiver) {
            if (attacker is Hero) {
                // --- Hero hits are send through NpcAI.MakeGetHitNoise since they have delay to allow sneaky kills.
                return;
            }
            Vector3 position = AINoises.GetPosition(attacker);
            var noiseRange = attacker.AIEntity is NpcAI npcAI
                ? npcAI.Data.perception.CombatHitsInformRange
                : SoundRange;
            if (receiver is IWithFaction withFaction) {
                if (!attacker.IsBlinded && !withFaction.IsHostileTo(attacker)) {
                    withFaction.TurnHostileTo(AntagonismLayer.Default, attacker);
                }
                AINoises.MakeNoise(noiseRange, NoiseStrength.VeryStrong, true, position, withFaction, attacker);
            } else {
                AINoises.MakeNoise(noiseRange, NoiseStrength.VeryStrong, true, position, attacker);
            }
        }

        void EnvironmentHit(bool calculateHitPoint, Collider colliderHit, RaycastHit hit, VLocation location, Item item, Vector3 direction, float ragdollForce) {
            Vector3 hitPoint = calculateHitPoint && colliderHit != null ? colliderHit.ClosestPointOnBounds(hit.point) : hit.point;
            Rigidbody rbHit = colliderHit != null ? colliderHit.GetComponentInParent<Rigidbody>() : null;
            EnvironmentHitData hitData = new() { Location = location, Item = item, Position = hitPoint, Direction = direction, Rigidbody = rbHit, RagdollForce = ragdollForce};
            ParentModel.Trigger(ICharacter.Events.HitEnvironment, hitData);
        }
        
        Damage GetDamageToDeal(Collider colliderHit, Item item, DamageParameters parameters, IAlive receiver, VLocation location = null) {
            // Regular damage dealt case
            var result = Damage.CalculateDamageDealt(dealer: ParentModel, receiver, parameters, item).WithHitCollider(colliderHit);
            
            // Handle tool interaction damage
            if (ParentModel is Hero { IsInToolAnimation: true } && item.HasElement<Tool>()) {
                parameters.DamageTypeData = new RuntimeDamageTypeData(DamageType.Interact);
                return new Damage(parameters, ParentModel, receiver, result.RawData).WithItem(item).WithHitCollider(colliderHit);
            }
            
            // Handle heavy attack
            if (parameters.IsHeavyAttack) {
                float heavyAttackDamageMultiplier = item.ItemStats.HeavyAttackDamageMultiplier.ModifiedValue;
                if (ParentModel is Hero h) {
                    float t = h.Elements<MeleeFSM>().FirstOrDefault(m => m.IsLayerActive)?.HeavyAttackChargePercent ?? 0;
                    heavyAttackDamageMultiplier += Mathf.Lerp(h.HeroStats.MinimumHeavyDamageAdd, h.HeroStats.MaximumHeavyDamageAdd, t);
                }
                result.RawData.MultiplyMultModifier(heavyAttackDamageMultiplier);
            }

            return result;
        }
        
        // === Helpers
        bool AlreadyDealtDamageWithItem(Item item, HealthElement healthElement) {
            return _damageDealtTo.TryGetValue(item, out List<HealthElement> elements) && elements.Contains(healthElement);
        }

        static bool TryGetAttackerCurrentCombatBehaviour(ICharacter character, out CombatEnemyBehaviourBase combatBehaviour) {
            if (character is NpcElement npc && npc.ParentModel.TryGetElement(out EnemyBaseClass enemy)) {
                if (enemy.CurrentBehaviour.Get() is CombatEnemyBehaviourBase cbb) {
                    combatBehaviour = cbb;
                    return true;
                }
            }

            combatBehaviour = null;
            return false;
        }
    }
}
