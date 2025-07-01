using Awaken.TG.Main.Animations.FSM.Heroes.Base;
using Awaken.TG.Main.Animations.FSM.Heroes.Machines;
using Awaken.TG.Main.Heroes;
using Awaken.TG.Main.Utility.Animations.HitStops;
using Awaken.TG.MVC;

namespace Awaken.TG.Main.Animations.FSM.Heroes.States.Melee {
    public partial class LightAttackForward : LightAttackBase {
        int _attackIndex = 1;
        
        public override HeroGeneralStateType GeneralType => HeroGeneralStateType.LightAttack;
        public override HeroStateType Type => HeroStateType.LightAttackForward;
        public override bool IsUsingMainHand => true;
        public override float EntryTransitionDuration => 0.1f;
        protected override bool IsHeavy => false;
        protected override HitStopData HitStopData => ParentModel is OneHandedFSM ? HitStopsAsset.lightAttack1HData : HitStopsAsset.lightAttack2HData;
        protected virtual float LightAttackCost => ParentModel.LightAttackCost;
        
        // === Life Cycle
        protected override bool BeforeEnter(out HeroStateType desiredState) {
            _attackIndex = _attackIndex == 1 ? 2 : 1;
            return base.BeforeEnter(out desiredState);
        }
        
        protected override void OnAfterEnter(float previousStateNormalizedTime) {
            Hero.Trigger(Hero.Events.DashForward, true);
            Stamina.DecreaseBy(LightAttackCost);
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