using System;
using System.Collections.Generic;
using Awaken.TG.Main.AI.Combat.Attachments;
using Awaken.TG.Main.AI.Combat.Behaviours.Abstracts;
using Awaken.TG.Main.AI.Movement.States;
using Awaken.TG.Main.Animations.FSM.Npc.Base;
using Awaken.TG.MVC;
using Awaken.Utility.Collections;

namespace Awaken.TG.Main.AI.Combat.Behaviours.BaseBehaviours {
    [Serializable]
    public partial class ParriedBehaviour : EnemyBehaviourBase, IInterruptBehaviour {
        public override int Weight => 0;
        public override bool CanBeInterrupted => false;
        public override bool AllowStaminaRegen => false;
        public override bool RequiresCombatSlot => false;
        public override bool CanBeAggressive => false;
        public override bool IsPeaceful => true;

        MovementState _overrideMovementState;
        
        protected override bool StartBehaviour() {
            ParentModel.SetAnimatorState(NpcStateType.Parried);
            _overrideMovementState = ParentModel.NpcElement.Movement.ChangeMainState(new NoMove());
            ParentModel.NpcElement.ListenToLimited(EnemyBaseClass.Events.ParriedAnimEnded, ExitParriedState, this);
            return true;
        }
        
        public override void StopBehaviour() {
            ParentModel.NpcElement.Movement.ResetMainState(_overrideMovementState);
        }

        public override bool UseConditionsEnsured() => false;

        void ExitParriedState(bool _) {
            if (!ParentModel.TryStartBehaviour<KeepPositionBehaviour>()) {
                ParentModel.StartWaitBehaviour();
            }
        }
        
        // === Editor
        public override EnemyBehaviourBase.Editor_Accessor GetEditorAccessor() => new Editor_Accessor(this);

        public new class Editor_Accessor : Editor_Accessor<ParriedBehaviour> {
            public override IEnumerable<NpcStateType> StatesUsedByThisBehaviour => NpcStateType.Parried.Yield();

            // === Constructor
            public Editor_Accessor(ParriedBehaviour behaviour) : base(behaviour) { }
        }
    }
}