using Awaken.TG.Main.AI.Combat.Attachments;
using Awaken.TG.Main.AI.Combat.Behaviours.Abstracts;
using Awaken.TG.Main.AI.Fights.Projectiles;
using Awaken.TG.Main.Animations.FSM.Heroes.Machines;
using Awaken.TG.Main.Animations.FSM.Heroes.Modifiers;
using Awaken.TG.Main.AudioSystem;
using Awaken.TG.Main.Character;
using Awaken.TG.Main.Fights.DamageInfo;
using Awaken.TG.Main.Fights.Modifiers;
using Awaken.TG.Main.Fights.NPCs;
using Awaken.TG.Main.General.Configs;
using Awaken.TG.Main.Heroes.Items;
using Awaken.TG.Main.Heroes.Items.Weapons;
using Awaken.TG.Main.Heroes.Stats;
using Awaken.TG.Main.Heroes.Stats.Controls;
using Awaken.TG.Main.Heroes.Statuses.Duration;
using Awaken.TG.Main.Utility.Animations;
using Awaken.TG.Main.Utility.UI;
using Awaken.TG.Main.Utility.VFX;
using Awaken.TG.MVC;
using Awaken.TG.MVC.Events;
using UnityEngine;

namespace Awaken.TG.Main.Heroes.Combat {
    public partial class HeroParry : DurationProxy<Hero> {
        const float StaminaReductionForParry = 15;
        [UnityEngine.Scripting.Preserve] const float DeflectedProjectileAimHeight = 0.65f;

        public sealed override bool IsNotSaved => true;
        
        public override IModel TimeModel => ParentModel;
        float StaminaDamageMultiplier => ParentModel.HeroStats.ParryStaminaDamageMultiplier.ModifiedValue;
        
        // === Events

        // === Constructor
        HeroParry(IDuration duration) : base(duration) {}
        
        // === Initialization
        protected override void OnInitialize() {
            ParentModel.HealthElement.ListenTo(HealthElement.Events.TakingDamage, OnTakingDamage, this);
            UpdateStatsItemAndConsumeParryStaminaCost();
            ParentModel.Trigger(ICharacter.Events.OnParryBegun, ParentModel);
        }

        void UpdateStatsItemAndConsumeParryStaminaCost() {
            ItemStats itemStats = HeroBlock.GetStatsItem(ParentModel)?.ItemStats;
            if (itemStats != null) {
                ParentModel.Stamina.DecreaseBy(itemStats.ParryStaminaCost);
            }
        }

        void RenewParry(IDuration duration) {
            UpdateStatsItemAndConsumeParryStaminaCost();
            Duration.Renew(duration);
        }
        
        // === Blocking Damage
        void OnTakingDamage(HookResult<HealthElement, Damage> hook) {
            if (HeroBlock.CanDamageBeParried(ParentModel, hook.Value, out Item statsItem, out Vector3 direction)) {
                hook.Value.SetParried(statsItem);
                hook.Prevent();
                
                ParentModel.HealthElement.Trigger(HealthElement.Events.BeforeDamageParried, hook.Value);
                ParentModel.Trigger(Hero.Events.HeroParriedDamage, hook.Value.Amount);
                ParentModel.HealthElement.Trigger(HealthElement.Events.OnDamageParried, hook.Value);
                
                ICharacter damageDealer = hook.Value.DamageDealer;
                damageDealer?.Trigger(HealthElement.Events.OnMyDamageBlocked, hook.Value);
                
                // --- Deal Stamina Damage
                var damageParameters = hook.Value.Parameters;
                if (damageParameters.KnockdownType == KnockdownType.Always) {
                    HeroKnockdown.EnterKnockdown(damageDealer, damageParameters.ForceDirection ?? Vector3.zero,
                        damageParameters.KnockdownStrength);
                }
                
                // --- Audio
                FMODManager.PlayBlockAudio(statsItem, ParentModel, hook.Value.Item, true);
                // --- VFX
                SurfaceType dealerSurface = hook.Value.Item?.DamageSurfaceType ?? SurfaceType.DamageMetal;
                SurfaceType hitSurface = statsItem.IsFists ? SurfaceType.HitFabric : statsItem.Template.DamageSurfaceType;
                VFXManager.SpawnCombatVFX(dealerSurface, hitSurface, hook.Value.Position ?? ParentModel.Coords, direction, ParentModel, hook.Value.HitCollider);
                // --- Vibration
                RewiredHelper.VibrateHighFreq(VibrationStrength.VeryStrong, VibrationDuration.Short);
                // --- Shake Camera
                ParentModel.Trigger(CameraShakesFSM.Events.ShakeHeroCamera, CameraShakeType.StrongAllDirection);
                
                // --- Apply Parry effects only if the damage item is not from projectile.
                if (hook.Value.Parameters.IsFromProjectile) {
                    TryDeflectProjectile(hook.Value);
                    return;
                }
                // --- Decrease Attacker Stamina
                if (damageDealer == null) {
                    return;
                }
                
                GameConstants gameConstants = GameConstants.Get;
                if (damageDealer is not { HasBeenDiscarded: false, IsAlive: true }) {
                    SlowDownTime.SlowTime(new TimeDuration(gameConstants.chonkyParrySlowMoDuration, true), gameConstants.chonkyParrySlowMoCurve, gameConstants.chonkyParryFovMultiplier);
                    return;
                }
                
                var dealerStamina = damageDealer.CharacterStats.Stamina;
                dealerStamina.DecreaseBy(StaminaReductionForParry * StaminaDamageMultiplier, new ContractContext(ParentModel, damageDealer, ChangeReason.CombatDamage));
                // --- Micro stagger attacker
                damageDealer.EnterParriedState();
                // --- Slow Down Time
                if (damageDealer is NpcElement npc && npc.ParentModel.TryGetElement(out EnemyBaseClass ebc) && ebc.Staggered) {
                    SlowDownTime.SlowTime(new TimeDuration(gameConstants.chonkyParrySlowMoDuration, true), gameConstants.chonkyParrySlowMoCurve, gameConstants.chonkyParryFovMultiplier);
                } else {
                    SlowDownTime.SlowTime(new TimeDuration(gameConstants.parrySlowMoDuration, true), gameConstants.parrySlowMoCurve, gameConstants.parryFovMultiplier);
                }
            }
        }

        void TryDeflectProjectile(Damage damage) {
            // --- Without info about projectile we cannot deflect it.
            Projectile projectile = damage.Projectile;
            if (projectile == null) {
                return;
            }

            // --- Player must have unlocked the ability to deflect projectiles.
            if (!ParentModel.Development.CanParryDeflectProjectiles) {
                return;
            }
            
            CharacterProjectileDeflection.GetOrCreate(ParentModel)
                                          .DeflectProjectile(damage, projectile, !ParentModel.Development.ParryDeflectionTargetsEnemies);
        }

        // === Public API
        public static void Parry(Hero hero, IDuration duration) {
            if (!hero.Development.CanParry) {
                return;
            }
            
            HeroParry prevent = hero.TryGetElement<HeroParry>();
            if (prevent != null) {
                prevent.RenewParry(duration);
            } else {
                hero.AddElement(new HeroParry(duration));
            }
        }
    }
}