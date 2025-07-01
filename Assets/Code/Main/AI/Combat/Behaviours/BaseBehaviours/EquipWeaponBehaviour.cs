using System;
using System.Collections.Generic;
using Awaken.TG.Code.Utility;
using Awaken.TG.Main.AI.Combat.Attachments;
using Awaken.TG.Main.AI.Combat.Behaviours.Abstracts;
using Awaken.TG.Main.AI.Idle.Interactions;
using Awaken.TG.Main.AI.Movement.States;
using Awaken.TG.Main.Animations.FSM.Npc.Base;
using Awaken.TG.Main.Animations.FSM.Npc.States.General;
using Awaken.TG.Main.Fights;
using Awaken.TG.Main.Fights.NPCs;
using Awaken.TG.Main.Heroes;
using Awaken.TG.Main.Heroes.Combat;
using Awaken.TG.Main.Heroes.Items;
using Awaken.TG.Main.Heroes.Items.Attachments;
using Awaken.TG.Main.Utility.Animations;
using Awaken.TG.Main.Utility.Animations.ARAnimator;
using Awaken.TG.MVC;
using UnityEngine;

namespace Awaken.TG.Main.AI.Combat.Behaviours.BaseBehaviours {
    [Serializable]
    public partial class EquipWeaponBehaviour : PersistentEnemyBehaviour, IInterruptBehaviour {
        const float ChanceToTauntOnExit = 0.5f;
        const NpcStateType MeleeStateType = NpcStateType.EquipWeapon;
        const NpcStateType RangedStateType = NpcStateType.EquipRangedWeapon;
        const NpcStateType CustomEquipStateType = NpcStateType.CustomEquipWeapon;

        public override int Weight => 0;
        public override bool CanBeInterrupted => false;
        public override bool AllowStaminaRegen => true;
        public override bool RequiresCombatSlot => false;
        public override bool CanBeAggressive => true;
        public override bool IsPeaceful => true;
        public bool EquippingMelee { get; private set; } = true;
        NpcStateType AnimatorState => _useCustomAnimation 
                                        ? CustomEquipStateType
                                        : (EquippingMelee ? MeleeStateType : RangedStateType);
        bool _useCustomAnimation;
        InteractionAnimationData _customInteractionData;
        bool _enterTauntOnExit = true;
        bool _weaponsAttached;
        bool _equippingWeaponsStarted;
        float? _overrideCrossFadeTime;

        protected override bool StartBehaviour() {
            _equippingWeaponsStarted = false;
            bool isInInteraction;
            if (Npc.Behaviours.CurrentUnwrappedInteraction is SimpleInteractionBase interaction) {
                _customInteractionData = interaction.InteractionAnimationData;
                isInInteraction = true;
            } else if (Npc.TryGetElement<SimpleInteractionExitMarker>(out var marker)) {
                _customInteractionData = marker.InteractionAnimationData;
                isInInteraction = true;
            } else {
                _customInteractionData = InteractionAnimationData.Default();
                isInInteraction = false;
            }
            
            _useCustomAnimation = _customInteractionData.customEquipWeapon != CustomEquipWeaponType.Default;
            
            if (isInInteraction) {
                if (_useCustomAnimation) {
                    _overrideCrossFadeTime = 0;
                    Npc.Trigger(NpcCustomEquipWeapon.Events.ChangeCombatExitToCombatState, _customInteractionData.customEquipWeapon);
                    StartEquipWeapon();
                } else {
                    _overrideCrossFadeTime = 1;
                }
            } else {
                _overrideCrossFadeTime = null;
                StartEquipWeapon();
            }
            
            _weaponsAttached = false;
            return true;
        }
        
        void StartEquipWeapon() {
            ParentModel.SetAnimatorState(AnimatorState, overrideCrossFadeTime: _overrideCrossFadeTime);
            bool rotateToTarget = !_useCustomAnimation || _customInteractionData.rotateToCombatTarget;
            ParentModel.NpcMovement.InterruptState(rotateToTarget ? new NoMoveAndRotateTowardsTarget() : new NoMove());
            _equippingWeaponsStarted = true;
        }
        
        public override void Update(float deltaTime) {
            if (!_equippingWeaponsStarted) {
                // --- Wait for AI exit from interaction
                if (Npc.Interactor.CurrentInteraction == null && !Npc.HasElement<SimpleInteractionExitMarker>()) {
                    StartEquipWeapon();
                }
                return;
            }
            
            if (NpcGeneralFSM.CurrentAnimatorState.Type != AnimatorState) {
                ExitEquipWeapon();
            }
        }

        public override void TriggerAnimationEvent(ARAnimationEvent animationEvent) {
            if (animationEvent.actionType == ARAnimationEvent.ActionType.AttachWeapon) {
                AttachWeaponsToHands(ParentModel.MainHandItem, ParentModel.OffHandItem, Npc);
                _weaponsAttached = true;
            }
        }

        public override void StopBehaviour() {
            ParentModel.NpcMovement.StopInterrupting();
        }

        protected override void BehaviourExit() {
            // --- If somebody forgot to add event to attach weapons attach them here
            if (!_weaponsAttached) {
                AttachWeaponsToHands(ParentModel.MainHandItem, ParentModel.OffHandItem, Npc);
            }
        }

        public void SetSettings(bool enterTauntOnExit, bool equipMelee) {
            _enterTauntOnExit = enterTauntOnExit;
            EquippingMelee = equipMelee;
        }

        public override bool UseConditionsEnsured() => false;

        void ExitEquipWeapon() {
            if (ParentModel.NpcAI is { InCombat: false }) {
                ParentModel.StopCurrentBehaviour(false);
                return;
            }
            
            if (!_enterTauntOnExit) {
                ParentModel.StopCurrentBehaviour(true);
                return;
            }
            
            if (!TryEnterTaunt()) {
                ParentModel.StopCurrentBehaviour(ParentModel.NpcAI?.InCombat ?? false);
            }
        }
        
        bool TryEnterTaunt() {
            var hero = Hero.Current;
            if (ParentModel.NpcElement.IsTargetingHero() && Vector3.Distance(hero.Coords, ParentModel.Coords) < VHeroCombatSlots.CombatSlotOffset * 2f) {
                return false;
            }
            TauntBehaviour tauntBehaviour = ParentModel.TryGetElement<TauntBehaviour>();
            if (tauntBehaviour != null && RandomUtil.WithProbability(ChanceToTauntOnExit)) {
                ParentModel.StartBehaviour(tauntBehaviour);
                return true;
            }

            return false;
        }

        public static void AttachWeaponsToHands(Item mainHandItem, Item offHandItem, NpcElement npc) {
            NpcWeaponsHandler weaponsHandler = npc.WeaponsHandler;
            if (weaponsHandler == null) {
                return;
            }
            
            if (mainHandItem != null) {
                weaponsHandler.AttachWeaponToHand(mainHandItem.Element<ItemEquip>());
            }
            if (offHandItem != null) {
                weaponsHandler.AttachWeaponToHand(offHandItem.Element<ItemEquip>());
            }
        }

        // === Editor
        public override EnemyBehaviourBase.Editor_Accessor GetEditorAccessor() => new Editor_Accessor(this);

        public new class Editor_Accessor : Editor_Accessor<EquipWeaponBehaviour> {
            public override IEnumerable<NpcStateType> StatesUsedByThisBehaviour => new[] { MeleeStateType, RangedStateType };

            // === Constructor
            public Editor_Accessor(EquipWeaponBehaviour behaviour) : base(behaviour) { }
        }
    }
}