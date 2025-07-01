using Awaken.TG.Main.Animations.FSM.Npc.Base;
using Awaken.Utility;

namespace Awaken.TG.Main.Animations.FSM.Npc.States.General {
    public partial class NpcLookAround : NpcAnimatorState {
        public override ushort TypeForSerialization => SavedModels.NpcLookAround;

        public override NpcStateType Type => NpcStateType.LookAround;

        protected override void OnUpdate(float deltaTime) {
            if (RemainingDuration <= 0.3f) {
                ParentModel.SetCurrentState(NpcStateType.Idle, 0.5f);
            }
        }
    }
}