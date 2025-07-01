using Awaken.TG.Main.Animations.FSM.Npc.Base;
using Awaken.Utility;

namespace Awaken.TG.Main.Animations.FSM.Npc.States.Alert {
    public partial class AlertStartQuick : NpcAnimatorState {
        public override ushort TypeForSerialization => SavedModels.AlertStartQuick;

        public override NpcStateType Type => NpcStateType.AlertStartQuick;

        protected override void AfterEnter(float previousStateNormalizedTime) {
            if (RemainingDuration < 10f) {
                // Don't stop transitioning if using fallback animation
                Npc.NpcAI.AlertStack.AlertTransitionsPaused = true;
            }
            Npc.NpcAI.ObserveAlertTarget = true;
        }

        protected override void OnExit(bool restarted) {
            Npc.NpcAI.AlertStack.AlertTransitionsPaused = false;
        }

        protected override void OnUpdate(float deltaTime) {
            if (RemainingDuration <= 0.15f) {
                ParentModel.SetCurrentState(AlertLookAt.GetStartingAlertState(Npc));
            }
        }
    }
}