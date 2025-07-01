using Awaken.TG.Main.AI.Combat.Attachments;
using Awaken.TG.Main.Animations.FSM.Npc.Base;
using Awaken.TG.Main.Fights;
using Awaken.TG.MVC;
using Awaken.Utility;

namespace Awaken.TG.Main.Animations.FSM.Npc.States.Combat {
    public partial class NpcStandUp : NpcAnimatorState {
        public override ushort TypeForSerialization => SavedModels.NpcStandUp;

        public override NpcStateType Type => NpcStateType.StandUp;
        public override bool CanUseMovement => true;
        public override bool CanBeExited => _canBeExited;
        public override bool CanReEnter => true;
        bool _canBeExited;
        
        protected override void AfterEnter(float previousStateNormalizedTime) {
            _canBeExited = false;
        }

        protected override void OnUpdate(float deltaTime) {
            if (RemainingDuration <= 0.3f) {
                _canBeExited = true;
                ParentModel.SetCurrentState(Npc.IsInCombat() ? NpcStateType.Wait : NpcStateType.Idle);
            }
        }

        protected override void OnExit(bool restarted) {
            ParentModel.ParentModel.Trigger(EnemyBaseClass.Events.StandUpFinished, ParentModel.ParentModel);
            base.OnExit(restarted);
        }
    }
}