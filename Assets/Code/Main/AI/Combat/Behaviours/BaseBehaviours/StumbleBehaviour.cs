using System;
using System.Collections.Generic;
using Awaken.TG.Main.AI.Combat.Behaviours.Abstracts;
using Awaken.TG.Main.AI.Movement.Controllers;
using Awaken.TG.Main.AI.Movement.States;
using Awaken.TG.Main.Animations.FSM.Npc.Base;
using Awaken.TG.Main.Saving;
using Unity.VisualScripting;

namespace Awaken.TG.Main.AI.Combat.Behaviours.BaseBehaviours {
    [Serializable]
    public partial class StumbleBehaviour : EnemyBehaviourBase, IInterruptBehaviour {
        public override int Weight => 0;
        public override bool CanBeInterrupted => false;
        public override bool AllowStaminaRegen => false;
        public override bool RequiresCombatSlot => false;
        public override bool CanBeAggressive => false;
        public override bool IsPeaceful => true;

        bool _isPush;
        Force _forceToApply;
        StumbleMovement _stumbleMovement;
        
        protected override bool StartBehaviour() {
            _stumbleMovement = new StumbleMovement(_forceToApply, true, _isPush);
            _stumbleMovement.exitToRagdoll += EnterRagdoll;
            ParentModel.NpcMovement.InterruptState(_stumbleMovement);
            ParentModel.SetAnimatorState(NpcStateType.StumbleOneStep);
            return true;
        }
        
        public override void Update(float deltaTime) {
            if (NpcGeneralFSM.CurrentAnimatorState.Type != NpcStateType.StumbleOneStep) {
                ParentModel.StartWaitBehaviour();
            }
        }
        
        public override void StopBehaviour() {
            ParentModel.NpcMovement.StopInterrupting();
            _stumbleMovement.exitToRagdoll -= EnterRagdoll;
        }

        public override bool UseConditionsEnsured() => false;
        
        // === Helpers
        public void SetStumbleParams(Force forceToApply, bool isPush) {
            _forceToApply = forceToApply;
            _isPush = isPush;
        }

        void EnterRagdoll(RagdollMovement ragdollMovement) {
            RagdollBehaviour rb = ParentModel.TryGetElement<RagdollBehaviour>();
            if (rb != null) {
                rb.SetRagdollParams(ragdollMovement);
                ParentModel.StartBehaviour(rb);
            }
        }
        
        // === Editor
        public override EnemyBehaviourBase.Editor_Accessor GetEditorAccessor() => new Editor_Accessor(this);

        public new class Editor_Accessor : Editor_Accessor<StumbleBehaviour> {
            public override IEnumerable<NpcStateType> StatesUsedByThisBehaviour => NpcStateType.StumbleOneStep.Yield();

            // === Constructor
            public Editor_Accessor(StumbleBehaviour behaviour) : base(behaviour) { }
        }
    }
}