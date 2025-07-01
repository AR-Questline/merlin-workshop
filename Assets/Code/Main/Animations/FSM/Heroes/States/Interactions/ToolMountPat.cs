using Awaken.TG.Main.Animations.FSM.Heroes.Base;
using Awaken.TG.Main.Animations.FSM.Heroes.Machines;

namespace Awaken.TG.Main.Animations.FSM.Heroes.States.Interactions {
    public partial class ToolMountPat : HeroAnimatorState<ToolInteractionFSM> {
        public override HeroGeneralStateType GeneralType => HeroGeneralStateType.General;
        public override HeroStateType Type => HeroStateType.PatMount;
        
        protected override void OnUpdate(float deltaTime) {
            if (TimeElapsedNormalized > 0.7f) {
                ParentModel.SetCurrentState(HeroStateType.None, 0f);
            }
        }
    }
}