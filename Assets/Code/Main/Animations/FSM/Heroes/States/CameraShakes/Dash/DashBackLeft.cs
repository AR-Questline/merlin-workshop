using Awaken.TG.Main.Animations.FSM.Heroes.Base;

namespace Awaken.TG.Main.Animations.FSM.Heroes.States.CameraShakes.Dash {
    public partial class DashBackLeft : DashBaseState {
        public override HeroGeneralStateType GeneralType => HeroGeneralStateType.General;
        public override HeroStateType Type => HeroStateType.DashBackLeft;
        
        public DashBackLeft(HeroStateType exitState) : base(exitState) { }
    }
}