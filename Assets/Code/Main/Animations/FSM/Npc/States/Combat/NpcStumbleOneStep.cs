using Awaken.TG.Main.Animations.FSM.Npc.Base;
using Awaken.Utility;

namespace Awaken.TG.Main.Animations.FSM.Npc.States.Combat {
    public partial class NpcStumbleOneStep : NpcAnimatorState {
        public override ushort TypeForSerialization => SavedModels.NpcStumbleOneStep;

        public override NpcStateType Type => NpcStateType.StumbleOneStep;

        protected override void OnUpdate(float deltaTime) {
            if (RemainingDuration <= 0.3f) {
                ParentModel.SetCurrentState(NpcStateType.Wait);
            }
        }
    }
}