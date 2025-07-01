using Awaken.TG.Main.Animations.FSM.Heroes.Base;

namespace Awaken.TG.Main.Animations.FSM.Heroes.States.CameraShakes.Dash {
    public partial class DashBackRight : DashBaseState {
        public override HeroGeneralStateType GeneralType => HeroGeneralStateType.General;
        public override HeroStateType Type => HeroStateType.DashBackRight;
        
        public DashBackRight(HeroStateType exitState) : base(exitState) { }
    }
}