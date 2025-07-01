using System;
using System.Collections.Generic;
using Awaken.TG.Main.AI.Combat.Behaviours.Abstracts;
using Awaken.TG.Main.AI.Movement.States;
using Awaken.TG.Main.Animations.FSM.Npc.Base;
using Awaken.Utility.Collections;

namespace Awaken.TG.Main.AI.Combat.Behaviours.BaseBehaviours {
    [Serializable]
    public partial class RagdollBehaviour : EnemyBehaviourBase, IInterruptBehaviour {
        public override int Weight => 0;
        public override bool CanBeInterrupted => false;
        public override bool AllowStaminaRegen => false;
        public override bool RequiresCombatSlot => false;
        public override bool CanBeAggressive => false;
        public override bool IsPeaceful => true;

        RagdollMovement _ragdollMovement;

        protected override bool StartBehaviour() {
            if (_ragdollMovement == null) {
                return false;
            }
            ParentModel.NpcMovement.InterruptState(_ragdollMovement);
            ParentModel.SetAnimatorState(NpcStateType.Idle, overrideCrossFadeTime: 0f);
            return true;
        }

        public override void StopBehaviour() {
            _ragdollMovement.ExitRagdoll(disableFallDamage: false);
            ParentModel.NpcMovement.StopInterrupting();
        }

        public override bool UseConditionsEnsured() => false;
        
        // === Helpers
        public void SetRagdollParams(RagdollMovement ragdollMovement) {
            _ragdollMovement = ragdollMovement;
        }
        
        // === Editor
        public override EnemyBehaviourBase.Editor_Accessor GetEditorAccessor() => new Editor_Accessor(this);

        public new class Editor_Accessor : Editor_Accessor<RagdollBehaviour> {
            public override IEnumerable<NpcStateType> StatesUsedByThisBehaviour => NpcStateType.StandUp.Yield();

            // === Constructor
            public Editor_Accessor(RagdollBehaviour behaviour) : base(behaviour) { }
        }
    }
}