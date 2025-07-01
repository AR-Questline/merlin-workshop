using System;
using Awaken.TG.Main.AI.Combat.Attachments.Bosses;
using Awaken.TG.Main.AI.Combat.Behaviours.Abstracts;
using Awaken.TG.Main.AI.Movement.States;
using Awaken.TG.Main.Animations.FSM.Npc.Base;
using Awaken.TG.Main.Saving;

namespace Awaken.TG.Main.AI.Combat.Behaviours.BossBehaviours {
    [Serializable]
    public partial class StrawDadThatchBurnBehaviour : CustomEnemyBehaviour<StrawDadCombat> {
        const float DissolveDuration = 7.5f;

        float _timeElapsed;

        public override bool CanMove => false;
        public override int Weight => 0;
        public override bool CanBeInterrupted => false;
        public override bool AllowStaminaRegen => false;
        public override bool RequiresCombatSlot => false;
        public override bool IsPeaceful => false;
        protected override NpcStateType StateType => NpcStateType.SpecialAttack;
        protected override NpcFSMType FSMType => NpcFSMType.OverridesFSM;
        float DissolveProgress => _timeElapsed / DissolveDuration;

        protected override bool OnStart() {
            ParentModel.NpcMovement.InterruptState(new NoMove());
            _timeElapsed = 0;
            return true;
        }

        public override void Update(float deltaTime) {
            _timeElapsed += deltaTime;
            ParentModel.UpdateThatchBurningProgress(DissolveProgress);
            
            if (NpcOverridesFSM.CurrentAnimatorState.Type != StateType) {
                ParentModel.StartWaitBehaviour();
                ParentModel.FinalizeBurning();
            }
        }

        public override bool UseConditionsEnsured() => false;

        protected override void BehaviourExit() {
            ParentModel.UpdateThatchBurningProgress(1f);
            ParentModel.NpcMovement.StopInterrupting();
        }
    }
}