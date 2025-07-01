using Awaken.TG.Main.Animations.FSM.Heroes.Base;

namespace Awaken.TG.Main.Animations.FSM.Heroes.States.Magic {
    public partial class MagicLightSequence : MagicLightBase, ISequencedLightAttack {
        int _attackIndex = -1;

        public override HeroGeneralStateType GeneralType => HeroGeneralStateType.MagicCastLight;
        public override HeroStateType Type => HeroStateType.MagicLightFirst;
        public override HeroStateType StateToEnter =>
            _attackIndex switch {
                1 => HeroStateType.MagicLightFirst,
                _ => HeroStateType.MagicLightSecond
            };
        public override float EntryTransitionDuration => 0.1f;
        public override bool CanReEnter => true;
        

        // === Public API
        public void ResetIndex() {
            _attackIndex = -1;
        }
        
        // === Life Cycle
        protected override bool BeforeEnter(out HeroStateType desiredState) {
            if (_attackIndex == -1) {
                _attackIndex = 1;
            } else {
                _attackIndex = _attackIndex > 1 ? 1 : 2;
            }
            return base.BeforeEnter(out desiredState);
        }
        
        protected override void OnUpdate(float deltaTime) {
            if (TimeElapsedNormalized >= 0.75f) {
                ParentModel.SetCurrentState(HeroStateType.Idle);
                return;
            }
            
            base.OnUpdate(deltaTime);
        }
    }
}