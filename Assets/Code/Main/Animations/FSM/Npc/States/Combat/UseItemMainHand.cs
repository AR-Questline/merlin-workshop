using Awaken.TG.Main.Animations.FSM.Npc.Base;
using Awaken.TG.Main.Animations.FSM.Npc.Machines;
using Awaken.Utility;

namespace Awaken.TG.Main.Animations.FSM.Npc.States.Combat {
    public partial class UseItemMainHand : NpcAnimatorState<NpcGeneralFSM> {
        public override ushort TypeForSerialization => SavedModels.UseItemMainHand;

        public override NpcStateType Type => NpcStateType.UseItemMainHand;

        protected override void OnUpdate(float deltaTime) {
            if (RemainingDuration <= 0.3f) {
                ParentModel.SetCurrentState(NpcStateType.Wait);
            }
        }
    }
}