using System;
using Awaken.TG.Main.AI.Combat.Attachments;
using Awaken.TG.Main.AI.Combat.Behaviours.BaseBehaviours;
using Awaken.TG.Main.AI.Movement.States;
using Awaken.TG.Main.Animations.FSM.Npc.Base;
using Awaken.TG.Main.Fights.Utils;
using Awaken.TG.Main.Heroes.Items;
using Awaken.TG.Main.Heroes.Items.Attachments;
using Awaken.TG.Main.Saving;
using Cysharp.Threading.Tasks;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Awaken.TG.Main.AI.Combat.Behaviours.ItemBasedBehaviours {
    [Serializable]
    public partial class ItemTemporaryEquippingBehaviourForwarder : ItemRequiringBehaviourForwarder {
        [FoldoutGroup("Equipping"), SerializeField] WeaponEquipHand overrideHand = WeaponEquipHand.None;
        [FoldoutGroup("Equipping"), SerializeField] EquipType equipType = EquipType.HideOnlyIfNeeded;
        [FoldoutGroup("Equip Animation"), SerializeField] bool playEquipAnimation;
        [FoldoutGroup("Equip Animation"), SerializeField, ShowIf(nameof(playEquipAnimation))] bool equipOnlyIfAnyWeaponHidden;
        [FoldoutGroup("Restore Animation"), SerializeField] bool playEquipAnimationOnRestoringWeapons = true;
        [FoldoutGroup("Restore Animation"), SerializeField, ShowIf(nameof(playEquipAnimationOnRestoringWeapons))] bool restoreOnlyIfAnyWeaponHidden = true;
        
        Item _equippedItem;
        // Equipping Animation
        NpcStateType _equippingAnimatorState;
        NoMoveAndRotateTowardsTarget _interruptState;
        bool _weaponWasHidden;

        public override bool Start() {
            EquipTemporaryWeapon();
            if (playEquipAnimation && (!equipOnlyIfAnyWeaponHidden || _weaponWasHidden)) {
                StartEquippingAnimation();
                return true;
            }
            return base.Start();
        }
        
        public override void Update(float deltaTime) {
            if (_equippingAnimatorState == default) {
                base.Update(deltaTime);
                return;
            }
            if (NpcGeneralFSM.CurrentAnimatorState.Type != _equippingAnimatorState) {
                OnEndEquippingAnimation();
            }
        }

        public override void StopBehaviour() {
            if (playEquipAnimationOnRestoringWeapons && (!restoreOnlyIfAnyWeaponHidden || _weaponWasHidden)) {
                UnequipTemporaryWeapon(false);
                ClearUpEquippingAnimation();
                base.StopBehaviour();
                DelayRestoreCurrentWeapons().Forget();
                return;
            }
            UnequipTemporaryWeapon();
            ClearUpEquippingAnimation();
            base.StopBehaviour();
        }

        public override void BehaviourInterrupted() {
            UnequipTemporaryWeapon();
            ClearUpEquippingAnimation();
            base.BehaviourInterrupted();
        }

        void EquipTemporaryWeapon() {
            _equippedItem = Item;
            ToggleCurrentWeapons(false);
            Npc.WeaponsHandler?.LoadNewWeapon(Item, true, overrideHand);
        }

        void UnequipTemporaryWeapon(bool restoreCurrentWeapons = true) {
            Npc.WeaponsHandler?.UnloadWeapon(_equippedItem);
            _equippedItem = null;
            if (restoreCurrentWeapons) {
                ToggleCurrentWeapons(true);
            }
        }

        void ToggleCurrentWeapons(bool enable) {
            if (equipType == EquipType.NeverHide) {
                return;
            }
            
            ItemEquip mainHand = Npc.Inventory.ItemInSlots[EquipmentSlotType.MainHand]?.TryGetElement<ItemEquip>();
            ItemEquip offHand = Npc.Inventory.ItemInSlots[EquipmentSlotType.OffHand]?.TryGetElement<ItemEquip>();
            bool hideAllWeapons = equipType == EquipType.HideAllOtherWeapons || overrideHand == WeaponEquipHand.None;
            bool hideMainHand = mainHand != null && (hideAllWeapons || equipType == EquipType.HideOnlyIfNeeded && (overrideHand == WeaponEquipHand.MainHand || mainHand.Item.IsTwoHanded));
            bool hideOffhand = offHand != null && (hideAllWeapons || equipType == EquipType.HideOnlyIfNeeded && (overrideHand == WeaponEquipHand.OffHand || (mainHand?.Item.IsTwoHanded ?? false)));
            
            if (enable) {
                if (hideMainHand) {
                    Npc.WeaponsHandler?.AttachWeaponToHand(mainHand);
                }
                if (hideOffhand) {
                    Npc.WeaponsHandler?.AttachWeaponToHand(offHand);
                }
                _weaponWasHidden = false;
            } else {
                _weaponWasHidden = hideMainHand || hideOffhand;
                if (hideMainHand) {
                    Npc.WeaponsHandler?.AttachWeaponToBelt(mainHand);
                }
                if (hideOffhand) {
                    Npc.WeaponsHandler?.AttachWeaponToBelt(offHand);
                }
            }
        }

        void StartEquippingAnimation() {
            bool isItemMelee = _equippedItem?.IsMelee ?? true;
            _equippingAnimatorState = isItemMelee ? NpcStateType.EquipWeapon : NpcStateType.EquipRangedWeapon;
            ParentModel.SetAnimatorState(_equippingAnimatorState);
            _interruptState = new NoMoveAndRotateTowardsTarget();
            ParentModel.NpcMovement.InterruptState(_interruptState);
        }

        void OnEndEquippingAnimation() {
            ClearUpEquippingAnimation();
            base.Start();
        }

        void ClearUpEquippingAnimation() {
            if (_equippingAnimatorState == default) {
                return;
            }
            _equippingAnimatorState = default;
            if (ParentModel.NpcMovement.CurrentState == _interruptState) {
                ParentModel.NpcMovement.StopInterrupting();
            }
            _interruptState = null;
        }
        
        async UniTaskVoid DelayRestoreCurrentWeapons() {
            if (!await AsyncUtil.DelayFrame(this)) {
                return;
            } 
            ParentModel.TryStartBehaviour<EquipWeaponBehaviour>();
        }

        internal enum EquipType : byte {
            HideAllOtherWeapons,
            HideOnlyIfNeeded,
            NeverHide,
        }
    }
}
