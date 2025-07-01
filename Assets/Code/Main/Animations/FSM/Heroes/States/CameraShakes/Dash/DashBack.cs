using Awaken.TG.Main.Animations.FSM.Heroes.Base;

namespace Awaken.TG.Main.Animations.FSM.Heroes.States.CameraShakes.Dash {
    public partial class DashBack : DashBaseState {
        public override HeroGeneralStateType GeneralType => HeroGeneralStateType.General;
        public override HeroStateType Type => HeroStateType.DashBack;
        
        public DashBack(HeroStateType exitState) : base(exitState) { }
    }
}