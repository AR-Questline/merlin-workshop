using Awaken.TG.Main.Animations.FSM.Heroes.Base;
using Awaken.TG.Main.Animations.FSM.Heroes.Machines;

namespace Awaken.TG.Main.Animations.FSM.Heroes.States.CameraShakes.Jump {
    public partial class JumpEndStrong : HeroAnimatorState<CameraShakesFSM> {
        public override HeroGeneralStateType GeneralType => HeroGeneralStateType.General;
        public override HeroStateType Type => HeroStateType.JumpEndStrong;

        protected override void OnUpdate(float deltaTime) {
            if (TimeElapsedNormalized > 0.9f) {
                ParentModel.SetCurrentState(HeroStateType.None, 0.1f);
            }
        }
    }
}