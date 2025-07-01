using Awaken.TG.Main.AI.Combat.Behaviours.Abstracts;
using Awaken.TG.Main.AudioSystem;
using Awaken.TG.Main.Character;
using Awaken.TG.Main.Fights.NPCs;
using Awaken.TG.Main.Fights.DamageInfo;
using Awaken.TG.Main.Grounds;
using Awaken.TG.Main.Heroes;
using Awaken.TG.Main.Heroes.Items;
using Awaken.TG.Main.Heroes.Stats;
using Awaken.TG.Main.Utility.Animations;
using Awaken.TG.MVC;
using Awaken.TG.MVC.Elements;
using Awaken.TG.MVC.Events;
using Awaken.TG.Utility;
using UnityEngine;

namespace Awaken.TG.Main.Fights {
    public partial class AIBlock : Element<NpcElement> {
        public sealed override bool IsNotSaved => true;

        protected override void OnInitialize() {
            ParentModel.HealthElement.ListenTo(HealthElement.Events.TakingDamage, OnTakingDamage, this);
            ParentModel.Trigger(ICharacter.Events.OnBlockBegun, ParentModel);
        }

        void OnTakingDamage(HookResult<HealthElement, Damage> hook) {
            if (!CanDamageBeBlocked(ParentModel, hook.Value, out Item statsItem)) {
                return;
            }
            
            ParentModel.HealthElement.Trigger(HealthElement.Events.BeforeDamageBlocked, hook.Value);
            
            float damageAmountMultiplier = 1;
            float blockValue = ParentModel.NpcStats.Block;
            if (blockValue > 0) {
                damageAmountMultiplier = (100 - blockValue) / 100f;
            }

            float originalAmount = hook.Value.RawData.CalculatedValue;
            ICharacter damageDealer = hook.Value.DamageDealer;
            hook.Value.RawData.MultiplyMultModifier(damageAmountMultiplier);
            if (hook.Value.Parameters.KnockdownType == KnockdownType.Always) {
                ParentModel.CharacterStats.Stamina.SetTo(0, false, new ContractContext(damageDealer, ParentModel, ChangeReason.CombatDamage));
            } else {
                float damagePrevented = originalAmount - hook.Value.Amount;
                hook.Value.StaminaDamageAmount = damagePrevented * ParentModel.NpcStats.BlockPenaltyMultiplier;
            }
            hook.Value.WithHitSurface(SurfaceType.HitWood);
            hook.Value.SetBlocked(statsItem);
            
            ParentModel.HealthElement.Trigger(HealthElement.Events.OnDamageBlocked, hook.Value);
            hook.Value.DamageDealer.Trigger(HealthElement.Events.OnMyDamageBlocked, hook.Value);
            // --- Audio
            FMODManager.PlayBlockAudio(statsItem, ParentModel, hook.Value.Item);
        }

        static bool CanDamageBeBlocked(NpcElement npcElement, Damage damage, out Item statsItem) {
            statsItem = null;

            if (!damage.CanBeBlocked) {
                return false;
            }

            if (!damage.IsPrimary) {
                return false;
            }

            if (damage.Item is { IsMelee: false }) {
                return false;
            }

            if (damage.Item is { IsSpectralWeapon: true } 
                && damage.DamageDealer is Hero { Development: { SpectralWeaponsPenetrateShields: true } }) {
                return false;
            }

            if (damage.DamageDealer == null) {
                return false;
            }

            statsItem = ItemUtils.GetStatsItemForBlock(npcElement);
            Stat blockAngle = statsItem?.ItemStats?.BlockAngle;
            float blockAngleValue = blockAngle?.ModifiedValue ?? 0;
            
            Vector3 direction = (damage.DamageDealer.Coords - npcElement.Coords).ToHorizontal3();
            float angle = Vector3.Angle(npcElement.Forward(), direction);
            return angle < blockAngleValue;
        }

        protected override void OnDiscard(bool fromDomainDrop) {
            if (!fromDomainDrop) {
                ParentModel.Trigger(ICharacter.Events.OnBlockEnded, ParentModel);
            }
            base.OnDiscard(fromDomainDrop);
        }
    }
}
