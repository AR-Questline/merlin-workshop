using Awaken.TG.Main.Animations.FSM.Npc.Base;
using Awaken.TG.Main.Animations.FSM.Npc.Machines;
using Awaken.Utility;

namespace Awaken.TG.Main.Animations.FSM.Npc.States.Custom {
    public partial class CustomStoryLoop : NpcAnimatorState<NpcCustomActionsFSM> {
        public override ushort TypeForSerialization => SavedModels.CustomStoryLoop;

        public override NpcStateType Type => NpcStateType.CustomStoryLoop;
        
        protected override void OnUpdate(float deltaTime) {
            if (ParentModel.StoryLoopTalking) {
                ParentModel.SetCurrentState(NpcStateType.CustomStoryLoopTalking, 0.6f);
            } else if (!ParentModel.StoryLoop) {
                ParentModel.SetCurrentState(NpcStateType.CustomLoop, 0.6f);
            }
        }
    }
}