using Awaken.TG.Main.Animations.FSM.Npc.Base;
using Awaken.TG.Main.Animations.FSM.Npc.Machines;
using Awaken.Utility;

namespace Awaken.TG.Main.Animations.FSM.Npc.States.Combat {
    public partial class UseItemOffHand : NpcAnimatorState<NpcGeneralFSM> {
        public override ushort TypeForSerialization => SavedModels.UseItemOffHand;

        public override NpcStateType Type => NpcStateType.UseItemOffHand;

        protected override void OnUpdate(float deltaTime) {
            if (RemainingDuration <= 0.3f) {
                ParentModel.SetCurrentState(NpcStateType.Wait);
            }
        }
    }
}