using Awaken.TG.Main.Animations.FSM.Heroes.Base;
using Awaken.TG.Main.Animations.FSM.Heroes.Machines;
using Awaken.TG.Main.Character;
using Awaken.TG.Main.Heroes;
using Awaken.TG.Main.Heroes.Items.Buffs;
using Awaken.TG.Main.Utility.Animations;
using Awaken.TG.MVC;
using Awaken.TG.MVC.Events;

namespace Awaken.TG.Main.Animations.FSM.Heroes.States.Overrides {
    public partial class HeroThrowableThrow : HeroAnimatorState<HeroOverridesFSM> {
        IEventListener _quickUseItemUsedListener;
        bool _thrown;
        
        public override HeroGeneralStateType GeneralType => HeroGeneralStateType.General;
        public override HeroStateType Type => HeroStateType.ThrowableThrow;
        public override bool CanPerformNewAction => false;
        public override bool CanReEnter => true;
        
        protected override void AfterEnter(float previousStateNormalizedTime) {
            _thrown = false;
            Hero.Trigger(Hero.Events.HideWeapons, true);
            _quickUseItemUsedListener = Hero.ListenTo(ICharacter.Events.OnQuickUseItemUsed, OnQuickUseItemUsedEvent, this);
            base.AfterEnter(previousStateNormalizedTime);
        }

        protected override void OnUpdate(float deltaTime) {
            if (TimeElapsedNormalized > 0.75f) {
                TryToThrow();
                ParentModel.SetCurrentState(HeroStateType.None, 0);
            }
        }
        
        protected override void OnExit(bool restarted) {
            Hero.Trigger(ItemThrowable.Events.ThrowableThrowAnimationEnded, true);
            World.EventSystem.DisposeListener(ref _quickUseItemUsedListener);
            if (!restarted) {
                Hero.Trigger(Hero.Events.ShowWeapons, true);
            }
            base.OnExit(restarted);
        }
        
        void OnQuickUseItemUsedEvent(ARAnimationEventData _) {
            TryToThrow();
        }

        void TryToThrow() {
            if (_thrown) {
                return;
            }
            _thrown = true;
            Hero.Trigger(ItemThrowable.Events.ThrowableThrown, true);
        }
    }
}