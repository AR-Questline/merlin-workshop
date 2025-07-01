using Awaken.CommonInterfaces.Animations;
using Awaken.TG.Main.AI.Idle;
using Awaken.TG.Main.Animations.FSM.Npc.Base;
using Awaken.TG.Main.Animations.FSM.Npc.Machines;
using Awaken.TG.Main.Heroes;
using Awaken.Utility;

namespace Awaken.TG.Main.Animations.FSM.Npc.States.Custom {
    public partial class CustomEnter : NpcAnimatorState<NpcCustomActionsFSM>, IAnimatorBridgeStateProvider {
        public override ushort TypeForSerialization => SavedModels.CustomEnter;

        public bool AlwaysAnimate => true;
        public override NpcStateType Type => NpcStateType.CustomEnter;

        protected override void OnUpdate(float deltaTime) {
            if (NpcTeleporter.HeroAllowsTeleporting(Hero.Current) || RemainingDuration <= 0.1f) {
                var state = ParentModel.StoryLoop ? NpcStateType.CustomStoryLoop : NpcStateType.CustomLoop;
                ParentModel.SetCurrentState(state, 0.1f);
            }
        }
    }
}