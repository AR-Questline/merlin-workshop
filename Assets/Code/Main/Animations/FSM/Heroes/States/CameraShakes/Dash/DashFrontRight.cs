using Awaken.TG.Main.Animations.FSM.Heroes.Base;

namespace Awaken.TG.Main.Animations.FSM.Heroes.States.CameraShakes.Dash {
    public partial class DashFrontRight : DashBaseState {
        public override HeroGeneralStateType GeneralType => HeroGeneralStateType.General;
        public override HeroStateType Type => HeroStateType.DashFrontRight;
        
        public DashFrontRight(HeroStateType exitState) : base(exitState) { }
    }
}