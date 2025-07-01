using Awaken.TG.Main.Animations.FSM.Npc.Base;
using Awaken.TG.MVC;
using Awaken.Utility;

namespace Awaken.TG.Main.Animations.FSM.Npc.States.Combat {
    public partial class DashBack : NpcAnimatorState {
        public override ushort TypeForSerialization => SavedModels.DashBack;

        public override NpcStateType Type => NpcStateType.DashBack;
        bool _eventTriggered;

        protected override void AfterEnter(float previousStateNormalizedTime) {
            _eventTriggered = false;
        }

        protected override void OnUpdate(float deltaTime) {
            if (RemainingDuration <= 0.3f && !_eventTriggered) {
                ParentModel.SetCurrentState(NpcStateType.Idle, 0.25f);
                Npc.Trigger(NpcAnimatorSubstateMachine.Events.NpcDashBackEnded, this);
                _eventTriggered = true;
            }
        }
    }
}