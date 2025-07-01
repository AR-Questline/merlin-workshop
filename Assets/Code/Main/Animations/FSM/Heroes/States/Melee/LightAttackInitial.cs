using Awaken.TG.Main.Animations.FSM.Heroes.Base;
using Awaken.TG.Main.Animations.FSM.Heroes.Machines;
using Awaken.TG.Main.Utility.Animations.HitStops;

namespace Awaken.TG.Main.Animations.FSM.Heroes.States.Melee {
    public partial class LightAttackInitial : LightAttackBase {
        public override HeroGeneralStateType GeneralType => HeroGeneralStateType.LightAttack;
        public override HeroStateType Type => HeroStateType.LightAttackInitial;
        public override bool IsUsingMainHand => true;
        protected override bool IsHeavy => false;
        protected override HitStopData HitStopData => ParentModel is OneHandedFSM or DualHandedFSM ? HitStopsAsset.lightAttack1HData : HitStopsAsset.lightAttack2HData;
        public override float EntryTransitionDuration => 0.1f;

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

        protected override void OnAfterExit() {
            foreach (var attack in ParentModel.Elements<ISequencedLightAttack>()) {
                attack.ResetIndex();
            }
        }
    }
}