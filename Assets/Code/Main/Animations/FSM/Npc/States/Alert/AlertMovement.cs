using Awaken.TG.Main.Animations.FSM.Npc.Base;
using Awaken.Utility;

namespace Awaken.TG.Main.Animations.FSM.Npc.States.Alert {
    public partial class AlertMovement : NpcAnimatorState {
        public override ushort TypeForSerialization => SavedModels.AlertMovement;

        public override NpcStateType Type => NpcStateType.AlertMovement;
        
        protected override void OnUpdate(float deltaTime) {
            if (NpcAnimancer.MovementSpeed < 0.05f) {
                ParentModel.SetCurrentState(NpcStateType.AlertLookAround);
            }
        }
    }
}