using Awaken.TG.Main.Animations.FSM.Heroes.Base;
using Awaken.TG.Main.Animations.FSM.Heroes.Machines;
using Awaken.TG.Main.Heroes.Combat;
using Awaken.TG.Main.Utility.Animations.HitStops;
using Awaken.TG.MVC;
using Awaken.TG.MVC.Events;

namespace Awaken.TG.Main.Animations.FSM.Heroes.States.BackStab {
    public partial class BackStabAttack : MeleeAttackAnimatorState {
        public override HeroGeneralStateType GeneralType => HeroGeneralStateType.BackStab;
        public override HeroStateType Type => HeroStateType.BackStabAttack;
        public override float EntryTransitionDuration => 0.1f;
        public override bool CanReEnter => false;
        public override bool IsUsingMainHand => true;
        protected override bool CanPerform => TimeElapsedNormalized > 0.9f;
        protected override bool IsHeavy => false;
        protected virtual float BackStabAttackCost => ParentModel.LightAttackCost;
        public override bool UsesActiveLayerMask => true;
        IEventListener _backStabEventListener;
        bool _triggered;

        protected override HitStopData HitStopData => ParentModel is OneHandedFSM or DualHandedFSM
            ? HitStopsAsset.lightAttack1HData
            : HitStopsAsset.lightAttack2HData;


        // === Life Cycle
        protected override void OnAfterEnter(float previousStateNormalizedTime) {
            Stamina.DecreaseBy(BackStabAttackCost);
            ParentModel.ResetAttackProlong();
            _triggered = false;
            _backStabEventListener = Hero.ListenTo(HeroHandOwner.Events.ReleaseBackStab, TryToPerformBackStab, this);
        }

        protected override void OnUpdate(float deltaTime) {
            base.OnUpdate(deltaTime);
            
            if (TimeElapsedNormalized >= 0.75f) {
                ParentModel.SetCurrentState(ParentModel.IsBackStabAvailable ? HeroStateType.BackStabLoop : HeroStateType.BackStabExit);
            }
        }

        protected override void OnExit(bool restarted) {
            if (_backStabEventListener != null) {
                World.EventSystem.DisposeListener(ref _backStabEventListener);
            }
            base.OnExit(restarted);
        }

        protected virtual void PerformBackStab() {
            Hero.VHeroController.BackStab(ParentModel.StatsItem);
        }

        void TryToPerformBackStab(HeroHandOwner _) {
            if (_triggered) {
                return;
            }
            PerformBackStab();
            _triggered = true;
        }
    }
}