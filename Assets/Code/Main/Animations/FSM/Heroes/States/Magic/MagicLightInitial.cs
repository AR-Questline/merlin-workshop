using Awaken.TG.Main.Animations.FSM.Heroes.Base;

namespace Awaken.TG.Main.Animations.FSM.Heroes.States.Magic {
    public partial class MagicLightInitial : MagicLightBase {
        public override HeroGeneralStateType GeneralType => HeroGeneralStateType.MagicCastLight;
        public override HeroStateType Type => HeroStateType.MagicLightInitial;
        public override float EntryTransitionDuration => 0.1f;
        
        protected override void OnUpdate(float deltaTime) {
            if (TimeElapsedNormalized >= 0.75f) {
                ParentModel.SetCurrentState(HeroStateType.Idle);
                return;
            }
            
            base.OnUpdate(deltaTime);
        }
        
        protected override void OnExit(bool restarted) {
            foreach (var attack in ParentModel.Elements<ISequencedLightAttack>()) {
                attack.ResetIndex();
            }
            base.OnExit(restarted);
        }
    }
}