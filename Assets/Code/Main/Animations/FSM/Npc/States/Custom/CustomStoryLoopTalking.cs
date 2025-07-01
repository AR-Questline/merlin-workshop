using Awaken.TG.Main.Animations.FSM.Npc.Base;
using Awaken.TG.Main.Animations.FSM.Npc.Machines;
using Awaken.Utility;

namespace Awaken.TG.Main.Animations.FSM.Npc.States.Custom {
    public partial class CustomStoryLoopTalking : NpcAnimatorState<NpcCustomActionsFSM> {
        public override ushort TypeForSerialization => SavedModels.CustomStoryLoopTalking;

        public override NpcStateType Type => NpcStateType.CustomStoryLoopTalking;
        
        protected override void OnUpdate(float deltaTime) {
            if (ParentModel.StoryLoopTalking) {
                return;
            }
            
            if (!ParentModel.StoryLoop) {
                ParentModel.SetCurrentState(NpcStateType.CustomLoop, 0.6f);
            } else if (!ParentModel.StoryLoopTalking) {
                ParentModel.SetCurrentState(NpcStateType.CustomStoryLoop, 0.6f);
            }
        }
    }
}