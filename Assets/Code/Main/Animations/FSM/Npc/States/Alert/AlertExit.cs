using Awaken.TG.Main.Animations.FSM.Npc.Base;
using Awaken.TG.Main.Fights.Modifiers;
using Awaken.TG.Main.Heroes.Statuses.Duration;
using Awaken.Utility;

namespace Awaken.TG.Main.Animations.FSM.Npc.States.Alert {
    public partial class AlertExit : NpcAnimatorState {
        public override ushort TypeForSerialization => SavedModels.AlertExit;

        public override NpcStateType Type => NpcStateType.AlertExit;
        
        protected override void AfterEnter(float previousStateNormalizedTime) {
            Npc.NpcAI.ObserveAlertTarget = false;
        }

        protected override void OnExit(bool restarted) { }

        protected override void OnUpdate(float deltaTime) {
            if (RemainingDuration <= 0.1f) {
                ParentModel.SetCurrentState(NpcStateType.Idle);
            }
        }
    }
}