using Awaken.TG.Main.Animations.FSM.Npc.Base;
using Awaken.TG.Main.Animations.FSM.Npc.Machines;
using Awaken.Utility;

namespace Awaken.TG.Main.Animations.FSM.Npc.States.Custom {
    public partial class CustomActionOverride : NpcAnimatorState<NpcOverridesFSM> {
        public override ushort TypeForSerialization => SavedModels.CustomActionOverride;

        public override NpcStateType Type => NpcStateType.CustomAction;
        
        protected override void OnUpdate(float deltaTime) {
            if (RemainingDuration <= 0.05f) {
                ParentModel.SetCurrentState(NpcStateType.None, 0f);
            }
        }
    }
}