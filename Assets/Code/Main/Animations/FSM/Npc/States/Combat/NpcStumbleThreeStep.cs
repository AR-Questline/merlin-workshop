using Awaken.TG.Main.Animations.FSM.Npc.Base;
using Awaken.Utility;

namespace Awaken.TG.Main.Animations.FSM.Npc.States.Combat {
    public partial class NpcStumbleThreeStep : NpcAnimatorState {
        public override ushort TypeForSerialization => SavedModels.NpcStumbleThreeStep;

        public override NpcStateType Type => NpcStateType.StumbleThreeStep;

        protected override void OnUpdate(float deltaTime) {
            if (RemainingDuration <= 0.3f) {
                ParentModel.SetCurrentState(NpcStateType.Wait);
            }
        }
    }
}