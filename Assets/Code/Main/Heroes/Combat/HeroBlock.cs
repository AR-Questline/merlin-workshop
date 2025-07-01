using Awaken.TG.Main.AI.Combat.Behaviours.Abstracts;
using Awaken.TG.Main.AudioSystem;
using Awaken.TG.Main.Character;
using Awaken.TG.Main.Fights.DamageInfo;
using Awaken.TG.Main.Grounds;
using Awaken.TG.Main.Heroes.Items;
using Awaken.TG.Main.Heroes.Items.Weapons;
using Awaken.TG.Main.Heroes.Stats;
using Awaken.TG.Main.Heroes.Stats.Controls;
using Awaken.TG.Main.Heroes.Statuses.Duration;
using Awaken.TG.Main.Saving;
using Awaken.TG.Main.Timing.ARTime;
using Awaken.TG.Main.Utility.Animations;
using Awaken.TG.Main.Utility.UI;
using Awaken.TG.Main.Utility.VFX;
using Awaken.TG.MVC;
using Awaken.TG.MVC.Elements;
using Awaken.TG.MVC.Events;
using Awaken.TG.Utility;
using UnityEngine;

namespace Awaken.TG.Main.Heroes.Combat {
    public partial class HeroBlock : Element<Hero> {
        [UnityEngine.Scripting.Preserve] public const float ExhaustDuration = 2;

        public sealed override bool IsNotSaved => true;
        
        float HoldBlockCostReduction => ParentModel.CharacterStats.HoldBlockCostReduction.ModifiedValue;
        float StaminaDamageMultiplier => ParentModel.HeroStats.BlockingStaminaDamageMultiplier.ModifiedValue;
        float ItemStaminaCostMultiplier => ParentModel.HeroStats.ItemStaminaCostMultiplier.ModifiedValue;
        [UnityEngine.Scripting.Preserve] float _staminaCostPerTick;
        
        protected override void OnInitialize() {
            ParentModel.HealthElement.ListenTo(HealthElement.Events.TakingDamage, OnTakingDamage, this);
            ItemStats itemStats = GetStatsItem(ParentModel)?.ItemStats;
            if (itemStats != null) {
                _staminaCostPerTick = itemStats.HoldItemCostPerTick.ModifiedValue * HoldBlockCostReduction * ParentModel.HeroStats.ItemStaminaCostMultiplier;
            }
            ParentModel.Trigger(ICharacter.Events.OnBlockBegun, ParentModel);
        }
        
        void OnTakingDamage(HookResult<HealthElement, Damage> hook) {
            if (ParentModel.HasElement<HeroParry>()) {
                return;
            }
            
            if (CanDamageBeBlocked(ParentModel, hook.Value, out Item statsItem, out Vector3 direction)) {
                float blockValue = statsItem?.ItemStats?.Block?.ModifiedValue ?? 0;
                blockValue *= ItemRequirementsUtils.GetBlockDamageReductionMultiplier(Hero.Current, statsItem);
                if (blockValue <= 0) {
                    return;
                }
                
                hook.Value.SetBlocked(statsItem);

                float originalIncomingDamage = hook.Value.Amount;
                ParentModel.HealthElement.Trigger(HealthElement.Events.BeforeDamageBlocked, hook.Value);
                ParentModel.Trigger(Hero.Events.HeroBlockedDamage, originalIncomingDamage);
                
                float damageAmountMultiplier = 1;
                if (blockValue > 0) {
                    damageAmountMultiplier = (100 - blockValue) / 100f;
                }
                hook.Value.RawData.MultiplyMultModifier(damageAmountMultiplier);
                
                ParentModel.HealthElement.Trigger(HealthElement.Events.OnDamageBlocked, hook.Value);
                ICharacter damageDealer = hook.Value.DamageDealer;
                damageDealer?.Trigger(HealthElement.Events.OnMyDamageBlocked, hook.Value);
                // --- Deal Stamina Damage
                var damageParameters = hook.Value.Parameters;
                if (damageParameters.KnockdownType == KnockdownType.Always) {
                    HeroKnockdown.EnterKnockdown(damageDealer, damageParameters.ForceDirection ?? Vector3.zero,
                        damageParameters.KnockdownStrength);
                } else if (statsItem?.ItemStats != null) {
                    float staminaLoseMultiplier = statsItem.ItemStats.BlockStaminaCostMultiplier * StaminaDamageMultiplier * ItemStaminaCostMultiplier;
                    ParentModel.Stamina.DecreaseBy(originalIncomingDamage * staminaLoseMultiplier);
                }
                // --- Audio
                FMODManager.PlayBlockAudio(statsItem, ParentModel, hook.Value.Item);
                // --- VFX
                SurfaceType dealerSurface = hook.Value.Item?.DamageSurfaceType ?? SurfaceType.DamageMetal;
                SurfaceType hitSurface = statsItem?.IsFists ?? true ? SurfaceType.HitFabric : statsItem.Template.DamageSurfaceType;
                VFXManager.SpawnCombatVFX(dealerSurface, hitSurface, hook.Value.Position ?? ParentModel.Coords, direction, ParentModel, hook.Value.HitCollider);
                // --- Vibration
                RewiredHelper.VibrateHighFreq(VibrationStrength.Medium, VibrationDuration.VeryShort);
                RewiredHelper.VibrateLowFreq(VibrationStrength.VeryStrong, VibrationDuration.Short);
                
                // Currently Disabled
                // --- Decrease Attacker Stamina
                //var dealerStamina = hook.Value.DamageDealer.CharacterStats.Stamina;
                // Was using StaminaDamageMultiplier, but the stat was reimplemented in other place
                //dealerStamina.DecreaseBy(StaminaReductionForBlock, new ContractContext(ParentModel, hook.Value.DamageDealer, ChangeReason.CombatDamage));
            }
        }

        protected override void OnDiscard(bool fromDomainDrop) {
            if (!fromDomainDrop) {
                ParentModel.Trigger(ICharacter.Events.OnBlockEnded, ParentModel);
            }
            base.OnDiscard(fromDomainDrop);
        }

        public static bool CanDamageBeBlocked(Hero hero, Damage damage, out Item statsItem, out Vector3 direction) {
            statsItem = null;
            direction = Vector3.zero;

            if (!damage.CanBeBlocked) {
                return false;
            }

            statsItem = GetStatsItem(hero);
            Stat blockAngle = statsItem?.ItemStats?.BlockAngle;
            float blockAngleValue = blockAngle?.ModifiedValue ?? 0;
            
            direction = (damage.DamageDealer.Coords - hero.Coords).ToHorizontal3();
            float angle = Vector3.Angle(hero.Forward(), direction);
            return angle < blockAngleValue;
        }

        public static bool CanDamageBeParried(Hero hero, Damage damage, out Item statsItem, out Vector3 direction) {
            bool cantDeflectProjectiles = !hero.Development.CanParryDeflectProjectiles;
            if (damage.Type == DamageType.MagicalHitSource && (!damage.Parameters.IsFromProjectile || cantDeflectProjectiles)) {
                statsItem = null;
                direction = Vector3.zero;
                return false;
            }
            return CanDamageBeBlocked(hero, damage, out statsItem, out direction);
        }
        
        public static Item GetStatsItem(Hero hero) {
            Item statsItem = hero.Inventory.EquippedItem(EquipmentSlotType.OffHand);
            if (statsItem == null || statsItem.IsFists) {
                statsItem = hero.Inventory.EquippedItem(EquipmentSlotType.MainHand);
            }
            return statsItem;
        }

        public static CharacterHandBase GetBlockingWeapon(Hero hero) {
            var blockingWeapon = hero.OffHandWeapon;
            if (blockingWeapon == null || blockingWeapon.Item.IsFists) {
                blockingWeapon = hero.MainHandWeapon;
            }
            return blockingWeapon;
        }
    }
}
