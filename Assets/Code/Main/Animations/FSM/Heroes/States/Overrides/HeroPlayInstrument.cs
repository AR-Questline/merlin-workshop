using Awaken.TG.Main.Animations.FSM.Heroes.Base;
using Awaken.TG.Main.Animations.FSM.Heroes.Machines;
using Awaken.TG.Main.Heroes;
using Awaken.TG.Main.Heroes.Items.Buffs;
using Awaken.TG.MVC;

namespace Awaken.TG.Main.Animations.FSM.Heroes.States.Overrides {
    public class HeroPlayInstrument : HeroAnimatorState<HeroOverridesFSM>, IAnimatorStateHeroInteraction {
        public override HeroGeneralStateType GeneralType => HeroGeneralStateType.General;
        public override HeroStateType Type => HeroStateType.PlayInstrument;
        public override bool CanPerformNewAction => false;
        public override bool CanReEnter => true;
        public bool IsInInteraction { get; private set; } = true;
        
        protected override void AfterEnter(float previousStateNormalizedTime) {
            Hero.Trigger(Hero.Events.HideWeapons, true);
            base.AfterEnter(previousStateNormalizedTime);
        }

        protected override void OnUpdate(float deltaTime) {
            if (TimeElapsedNormalized > 0.99f) {
                ParentModel.SetCurrentState(HeroStateType.None, 0f);
            }
        }
        
        protected override void OnExit(bool restarted) {
            Hero.Trigger(ItemInstrument.Events.PlayingInstrumentEnded, true);
            if (!restarted && !Hero.IsWeaponEquipped) {
                IsInInteraction = false;
                Hero.Trigger(Hero.Events.ShowWeapons, true);
                IsInInteraction = true;
            }
            base.OnExit(restarted);
        }
    }
}