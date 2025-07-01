using Awaken.TG.Main.Animations.FSM.Heroes.Base;
using Awaken.TG.Main.Animations.FSM.Heroes.Machines;
using Awaken.TG.Main.Utility.Animations.HitStops;

namespace Awaken.TG.Main.Animations.FSM.Heroes.States.Melee {
    public partial class LightAttack : LightAttackBase, ISequencedLightAttack {
        int _attackIndex = -1;
        
        public override HeroGeneralStateType GeneralType => HeroGeneralStateType.LightAttack;
        public override HeroStateType Type => HeroStateType.LightAttackFirst;
        public override HeroStateType StateToEnter => IsUsingMainHand ? HeroStateType.LightAttackSecond : HeroStateType.LightAttackFirst;
        public override float EntryTransitionDuration => 0.1f;
        public override bool CanReEnter => true;
        public override bool IsUsingMainHand => _attackIndex != 1;
        protected override bool CanPerform => Entered && base.CanPerform;
        protected override bool IsHeavy => false;
        protected override HitStopData HitStopData => ParentModel is OneHandedFSM or DualHandedFSM ? HitStopsAsset.lightAttack1HData : HitStopsAsset.lightAttack2HData;
        
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
        
        protected override void OnAfterEnter(float previousStateNormalizedTime) {
            Stamina.DecreaseBy(ParentModel.LightAttackCost);
            ParentModel.ResetAttackProlong();
        }

        protected override void OnUpdate(float deltaTime) {
            base.OnUpdate(deltaTime);
            
            if (TimeElapsedNormalized >= 0.75f) {
                ParentModel.SetCurrentState(HeroStateType.Idle);
            }
        }
    }
}