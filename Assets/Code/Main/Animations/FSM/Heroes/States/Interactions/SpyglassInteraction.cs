using Awaken.TG.Main.Animations.FSM.Heroes.Base;
using Awaken.TG.Main.Animations.FSM.Heroes.Machines;

namespace Awaken.TG.Main.Animations.FSM.Heroes.States.Interactions {
    public partial class SpyglassInteraction : HeroAnimatorState<SpyglassFSM> {
        public override HeroGeneralStateType GeneralType => HeroGeneralStateType.General;
        public override HeroStateType Type => HeroStateType.ToolInteract;
    }
}