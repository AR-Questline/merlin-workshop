using Awaken.TG.Main.Animations.FSM.Npc.Base;
using Awaken.TG.Main.Animations.FSM.Npc.Machines;
using Awaken.Utility;

namespace Awaken.TG.Main.Animations.FSM.Npc.States.Combat {
    public sealed partial class GetHit : NpcAnimatorState {
        public override ushort TypeForSerialization => SavedModels.GetHit;

        public override NpcStateType Type => NpcStateType.GetHit;

        protected override void OnUpdate(float deltaTime) {
            if (RemainingDuration <= 0.3f) {
                ParentModel.SetCurrentState(NpcStateType.None);
            }
        }
    }
}