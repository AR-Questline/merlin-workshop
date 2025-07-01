using Awaken.TG.Main.Animations.FSM.Heroes.Base;

namespace Awaken.TG.Main.Animations.FSM.Heroes.States.CameraShakes.Dash {
    public partial class DashFrontLeft : DashBaseState {
        public override HeroGeneralStateType GeneralType => HeroGeneralStateType.General;
        public override HeroStateType Type => HeroStateType.DashFrontLeft;        
        
        public DashFrontLeft(HeroStateType exitState) : base(exitState) { }
    }
}