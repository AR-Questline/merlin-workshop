using Awaken.TG.Main.Animations.FSM.Heroes.Base;
using Awaken.TG.Main.Animations.FSM.Heroes.Machines;

namespace Awaken.TG.Main.Animations.FSM.Heroes.States.BackStab {
    public partial class BackStabLoop : HeroAnimatorState<MeleeFSM> {
        public override HeroGeneralStateType GeneralType => HeroGeneralStateType.BackStab;
        public override HeroStateType Type => HeroStateType.BackStabLoop;
        public override bool UsesActiveLayerMask => true;
        
        protected override float OffsetNormalizedTime(float previousNormalizedTime) => ParentModel.SynchronizedStateOffsetNormalizedTime();
        
        protected override void OnUpdate(float deltaTime) {
            if (!ParentModel.IsBackStabAvailable) {
                ParentModel.SetCurrentState(HeroStateType.BackStabExit);
            }
        }
    }
}