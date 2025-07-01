using System;
using System.Linq;
using Awaken.TG.Main.Animations.FSM.Npc.Base;
using Awaken.TG.Main.AudioSystem;
using Awaken.TG.Main.Character;
using Awaken.TG.Main.Fights.DamageInfo;
using Awaken.TG.Main.Heroes.Items;
using Awaken.TG.Main.Heroes.Items.Attachments;
using Awaken.TG.MVC;
using Awaken.Utility;

namespace Awaken.TG.Main.AI.Combat.Attachments.Customs {
    [Serializable]
    public partial class ShieldMan : CustomCombatBaseClass {
        public override ushort TypeForSerialization => SavedModels.ShieldMan;
        
        protected override void OnInitialize() {
            base.OnInitialize();
            ParentModel.AfterFullyInitialized(OnParentFullyInitialized);
        }

        void OnParentFullyInitialized() {
            NpcElement.HealthElement.ListenTo(HealthElement.Events.DamagePreventedByHitbox, OnDamagePreventedByHitbox, this);
        }
        
        void OnDamagePreventedByHitbox(Damage damage) {
            // --- Show reaction to hit but only as additive animation so it doesn't interrupt current animation.
            SetAnimatorState(NpcStateType.GetHit, NpcFSMType.AdditiveFSM, 0f);
            // --- Audio
            FMODManager.PlayBlockAudio(ItemUtils.GetStatsItemForBlock(NpcElement), NpcElement, damage.Item);
        }
        
        public override void RefreshFightingStyle() {
            base.RefreshFightingStyle();
            if (WeaponsEquipped && NpcElement.TryGetElement(out NpcWeaponsHandler weaponsHandler)) {
                ItemEquip shield = NpcElement.Inventory.Items.FirstOrDefault(i => i.IsShield)?.TryGetElement<ItemEquip>();
                if (shield == null) {
                    return;
                }
                weaponsHandler.AttachWeaponToHand(shield);
            }
        }
    }
}