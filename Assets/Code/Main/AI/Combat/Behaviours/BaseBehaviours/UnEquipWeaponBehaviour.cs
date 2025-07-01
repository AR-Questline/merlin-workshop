using System.Collections.Generic;
using Awaken.TG.Main.AI.Combat.Behaviours.Abstracts;
using Awaken.TG.Main.AI.Movement.States;
using Awaken.TG.Main.Animations.FSM.Npc.Base;
using Awaken.TG.Main.Character;
using Awaken.TG.Main.Fights.NPCs;
using Awaken.TG.Main.Heroes.Items;
using Awaken.TG.Main.Utility.Animations;

namespace Awaken.TG.Main.AI.Combat.Behaviours.BaseBehaviours {
    public partial class UnEquipWeaponBehaviour : PersistentEnemyBehaviour, IInterruptBehaviour {
        const NpcStateType AnimatorState = NpcStateType.UnequipWeapon;
        
        // === Fields
        public bool isExitingToCombat;
        bool _weaponsUnAttached;
        NoMove _noMove;
        
        // === Properties
        public override int Weight => 0;
        public override bool CanBeInterrupted => false;
        public override bool AllowStaminaRegen => true;
        public override bool RequiresCombatSlot => false;
        public override bool CanBeAggressive => true;
        public override bool IsPeaceful => true;

        protected override void OnInitialize() {
            _noMove = new NoMove();
            base.OnInitialize();
        }

        protected override bool StartBehaviour() {
            isExitingToCombat = false;
            _weaponsUnAttached = false;
            StartUnEquipWeapon();
            return true;
        }
        
        void StartUnEquipWeapon() {
            ParentModel.SetAnimatorState(AnimatorState);
            ParentModel.NpcMovement.InterruptState(_noMove);
        }
        
        public override void Update(float deltaTime) {
            if (NpcGeneralFSM.CurrentAnimatorState.Type != AnimatorState) {
                ExitUnEquipWeapon();
            }
        }

        public override void TriggerAnimationEvent(ARAnimationEvent animationEvent) {
            if (animationEvent.actionType == ARAnimationEvent.ActionType.AttachWeapon) {
                AttachWeaponsToBelts(Npc);
                _weaponsUnAttached = true;
            }
        }

        public override void StopBehaviour() {
            ParentModel.NpcMovement.StopInterrupting();
        }

        public override bool UseConditionsEnsured() => false;

        protected override void BehaviourExit() {
            // --- If somebody forgot to add event to attach weapons attach them here
            if (!_weaponsUnAttached) {
                AttachWeaponsToBelts(Npc);
            }
        }

        void ExitUnEquipWeapon() {
            if (ParentModel.NpcAI is { InCombat: false }) {
                ParentModel.StopCurrentBehaviour(false);
            } else {
                ParentModel.TryToStartNewBehaviourExcept(this);
            }
        }

        void AttachWeaponsToBelts(NpcElement npc) {
            if (isExitingToCombat) {
                return;
            }
            
            if (ParentModel.NpcAI is { InCombat: true }) {
                return;
            }
            
            npc.Inventory.Unequip(EquipmentSlotType.MainHand);
            npc.Inventory.Unequip(EquipmentSlotType.OffHand);
        }

        // === Editor
        public override EnemyBehaviourBase.Editor_Accessor GetEditorAccessor() => new Editor_Accessor(this);

        public new class Editor_Accessor : Editor_Accessor<UnEquipWeaponBehaviour> {
            public override IEnumerable<NpcStateType> StatesUsedByThisBehaviour => new[] { AnimatorState };

            // === Constructor
            public Editor_Accessor(UnEquipWeaponBehaviour behaviour) : base(behaviour) { }
        }
    }
}