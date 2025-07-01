using Awaken.TG.Main.Character;
using Awaken.TG.Main.Fights.DamageInfo;
using Awaken.TG.Main.Heroes.Items;
using Awaken.TG.MVC;
using Awaken.TG.MVC.Elements;
using Awaken.TG.MVC.Events;
using UnityEngine;

namespace Awaken.TG.Main.Heroes.Combat {
    public partial class HeroHandOwner : Element<Hero>, IHandOwner<Hero> {
        public sealed override bool IsNotSaved => true;

        public const float AdditionalHitOffset = 0.25f;
        
        public float WeaponColliderDivider => 2.5f;
        public LayerMask HitLayerMask => ParentModel.Data.enemiesHitMask;

        public new static class Events {
            public static readonly Event<Hero, HeroHandOwner> ReleaseBackStab = new(nameof(ReleaseBackStab));
        }
        
        protected override void OnInitialize() {
            ParentModel.Inventory.ListenTo(ICharacterInventory.Events.SlotChanged(EquipmentSlotType.MainHand), OnWeaponChanged, this);
        }

        void OnWeaponChanged(ICharacterInventory inventory) {
            Item mainHandItem = inventory.EquippedItem(EquipmentSlotType.MainHand);
            mainHandItem?.FinisherType.SpawnFinisher(ParentModel, mainHandItem);
        }
            
        // === IHandOwner
        public void OnFinisherRelease(Vector3 weaponPosition) {
            ParentModel.TryGetElement<IFinisher>()?.Release(weaponPosition);
        }

        public void OnBackStabRelease() {
            ParentModel.Trigger(Events.ReleaseBackStab, this);
        }
        
        public void OnWeaponTriggerEnter(RaycastHit hit, in AttackParameters attackParameters, bool inEnviroHitRange) {
            if (!IsHitValid(out var characterDealingDamage)) {
                return;
            }

            hit.point = hit.collider != null ? hit.collider.ClosestPointOnBounds(hit.point) : hit.point;
            if (ParentModel.IsInDualHandedAttack) {
                GetAdditionalHit(hit, attackParameters, out RaycastHit additionalHit, out AttackParameters additionalParameters);
                characterDealingDamage.OnWeaponTriggerEnter(additionalHit, additionalParameters, inEnviroHitRange, false);
            }
            characterDealingDamage.OnWeaponTriggerEnter(hit, attackParameters, inEnviroHitRange, false);
        }
        
        public void OnMagicGauntletTriggerEnter(RaycastHit hit, in AttackParameters attackParameters, bool inEnviroHitRange) {
            if (!IsHitValid(out var characterDealingDamage)) {
                return;
            }

            hit.point = hit.collider != null ? hit.collider.ClosestPointOnBounds(hit.point) : hit.point;
            if (ParentModel.IsInDualHandedAttack) {
                GetAdditionalHit(hit, attackParameters, out RaycastHit additionalHit, out AttackParameters additionalParameters);
                characterDealingDamage.OnMagicGauntletTriggerEnter(additionalHit, additionalParameters, inEnviroHitRange, false);
            }
            characterDealingDamage.OnMagicGauntletTriggerEnter(hit, attackParameters, inEnviroHitRange, false);
        }

        bool IsHitValid(out CharacterDealingDamage characterDealingDamage) {
            if (ParentModel.IsInHitStop) {
                characterDealingDamage = null;
                return false;
            }
            
            characterDealingDamage = ParentModel.CharacterDealingDamage;
            if (characterDealingDamage == null) {
                return false;
            }

            return true;
        }

        void GetAdditionalHit(RaycastHit hit, AttackParameters attackParameters, out RaycastHit additionalHit, out AttackParameters additionalParameters) {
            Item mainHandItem = ParentModel.MainHandItem;
            bool mainHitWithMainHand = attackParameters.Item == mainHandItem;
            Item additionalItem = attackParameters.Item == mainHandItem ? ParentModel.OffHandItem : mainHandItem;
            additionalHit = hit;
            Vector3 hitPoint = hit.point;
            hitPoint += ParentModel.ParentTransform.right * (mainHitWithMainHand ? -AdditionalHitOffset : AdditionalHitOffset);
            additionalHit.point = hitPoint;
            additionalParameters = new(attackParameters.ICharacter, additionalItem,
                attackParameters.AttackType, attackParameters.AttackDirection);
        }
    }
}