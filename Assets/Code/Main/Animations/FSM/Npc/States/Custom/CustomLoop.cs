using Awaken.TG.Main.Animations.FSM.Npc.Base;
using Awaken.TG.Main.Animations.FSM.Npc.Machines;
using Awaken.TG.Main.Fights.NPCs;
using Awaken.TG.MVC;
using Awaken.TG.MVC.Events;
using Awaken.Utility;

namespace Awaken.TG.Main.Animations.FSM.Npc.States.Custom {
    public partial class CustomLoop : NpcAnimatorState<NpcCustomActionsFSM> {
        public override ushort TypeForSerialization => SavedModels.CustomLoop;

        bool _loopEndedEventFired;
        
        public override NpcStateType Type => NpcStateType.CustomLoop;
        
        public new static class Events {
            public static readonly Event<NpcElement, bool> CustomLoopEnded = new(nameof(CustomLoopEnded));
        }
        
        protected override void OnUpdate(float deltaTime) {
            if (ParentModel.StoryLoopTalking) {
                ParentModel.SetCurrentState(NpcStateType.CustomStoryLoopTalking, 0.6f);
            } else if (ParentModel.StoryLoop) {
                ParentModel.SetCurrentState(NpcStateType.CustomStoryLoop, 0.6f);
            }

            if (TimeElapsedNormalized % 1 >= 0.95f) {
                if (!_loopEndedEventFired) {
                    _loopEndedEventFired = true;
                    Npc.Trigger(Events.CustomLoopEnded, true);
                }
            } else if (_loopEndedEventFired) {
                _loopEndedEventFired = false;
            }
        }
    }
}