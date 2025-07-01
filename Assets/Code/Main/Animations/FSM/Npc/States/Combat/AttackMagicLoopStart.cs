using Awaken.TG.Main.Animations.FSM.Npc.Base;
using Awaken.Utility;

namespace Awaken.TG.Main.Animations.FSM.Npc.States.Combat {
    public partial class AttackMagicLoopStart : NpcAnimatorState {
        public override ushort TypeForSerialization => SavedModels.AttackMagicLoopStart;

        public override NpcStateType Type => NpcStateType.MagicLoopStart;
        
        protected override void OnUpdate(float deltaTime) {
            if (RemainingDuration <= 0.3f) {
                ParentModel.SetCurrentState(NpcStateType.MagicLoopHold);
            }
        }
    }
}