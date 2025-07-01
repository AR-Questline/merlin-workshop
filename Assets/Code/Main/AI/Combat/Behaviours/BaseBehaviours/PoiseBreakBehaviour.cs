using System;
using System.Collections.Generic;
using Awaken.TG.Main.AI.Combat.Behaviours.Abstracts;
using Awaken.TG.Main.AI.Movement.States;
using Awaken.TG.Main.Animations.FSM.Npc.Base;
using Awaken.TG.Main.Fights;
using Awaken.TG.MVC;

namespace Awaken.TG.Main.AI.Combat.Behaviours.BaseBehaviours {
    [Serializable]
    public partial class PoiseBreakBehaviour : EnemyBehaviourBase, IInterruptBehaviour {
        public override int Weight => 0;
        public override bool CanBeInterrupted => false;
        public override bool AllowStaminaRegen => false;
        public override bool RequiresCombatSlot => false;
        public override bool CanBeAggressive => false;
        public override bool IsPeaceful => true;

        NpcStateType _stateToEnter = NpcStateType.PoiseBreakFront;
        
        // === LifeCycle
        protected override bool StartBehaviour() {
            ParentModel.SetAnimatorState(_stateToEnter);
            ParentModel.NpcElement.Movement.InterruptState(new NoMove());
            return true;
        }
        
        public override void Update(float deltaTime) {
            if (NpcGeneralFSM.CurrentAnimatorState.Type != _stateToEnter) {
                OnAnimatorExitDesiredState();
            }
        }
        
        public override void StopBehaviour() {
            ParentModel.NpcElement.Movement.StopInterrupting();
            World.EventSystem.RemoveAllListenersOwnedBy(this);
        }
        
        // === Public API
        public void UpdatePoiseBreakDirection(NpcStateType stateType) {
            _stateToEnter = stateType;
        }

        public override bool UseConditionsEnsured() => false;
        
        // === Listener Callbacks
        void OnAnimatorExitDesiredState() {
            if (!ParentModel.NpcElement.IsInCombat()) {
                ParentModel.StopCurrentBehaviour(false);
                return;
            }
            
            if (!ParentModel.TryStartBehaviour<KeepPositionBehaviour>()) {
                ParentModel.StartWaitBehaviour();
            }
        }
        
        // === Editor
        public override EnemyBehaviourBase.Editor_Accessor GetEditorAccessor() => new Editor_Accessor(this);

        public new class Editor_Accessor : Editor_Accessor<PoiseBreakBehaviour> {
            public override IEnumerable<NpcStateType> StatesUsedByThisBehaviour => new[] {
                NpcStateType.PoiseBreakFront, 
                NpcStateType.PoiseBreakBackLeft,
                NpcStateType.PoiseBreakBack,
                NpcStateType.PoiseBreakBackRight, 
            };

            // === Constructor
            public Editor_Accessor(PoiseBreakBehaviour behaviour) : base(behaviour) { }
        }
    }
}