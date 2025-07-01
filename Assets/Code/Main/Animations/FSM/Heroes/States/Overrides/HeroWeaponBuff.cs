using Awaken.TG.Main.Animations.FSM.Heroes.Base;
using Awaken.TG.Main.Animations.FSM.Heroes.Machines;
using Awaken.TG.Main.Heroes.Combat;
using Awaken.TG.Main.Heroes.Items.Buffs;
using Awaken.TG.MVC;
using Awaken.TG.MVC.Events;
using Awaken.TG.Utility;

namespace Awaken.TG.Main.Animations.FSM.Heroes.States.Overrides {
    public partial class HeroWeaponBuff : HeroAnimatorState<HeroOverridesFSM> {
        const float EffectDelay = 0.25f, EffectEnd = 0.5f;
        
        public override HeroGeneralStateType GeneralType => HeroGeneralStateType.General;
        public override HeroStateType Type => HeroStateType.WeaponBuff;
        static float EffectValue(float timeElapsed) => timeElapsed.Remap(EffectDelay, EffectEnd, 0, 1f);
        
        IEventListener _mainHandChangedEvent;
        IEventListener _offHandChangedEvent;

        protected override void AfterEnter(float previousStateNormalizedTime) {
            base.AfterEnter(previousStateNormalizedTime);

            if (Hero.MainHandItem is { } mainHandItem) {
                _mainHandChangedEvent = mainHandItem.ListenTo(CharacterHandBase.Events.WeaponDestroyed, OnHeldWeaponChanged, this);
            }
            if (Hero.OffHandItem is { } offHandItem) {
                _offHandChangedEvent = offHandItem.ListenTo(CharacterHandBase.Events.WeaponDestroyed, OnHeldWeaponChanged, this);
            }
        }

        void OnHeldWeaponChanged() {
            ParentModel.SetCurrentState(HeroStateType.None);
        }

        protected override void OnUpdate(float deltaTime) {
            if (TimeElapsedNormalized > 0.75f) {
                ParentModel.SetCurrentState(HeroStateType.None);
                Hero.Trigger(AppliedItemBuff.Events.WeaponBuffVFXUpdate, 1f);
                Hero.Trigger(AppliedItemBuff.Events.WeaponBuffVFXUpdateCompleted, true);
            } else if (TimeElapsedNormalized > EffectDelay) {
                Hero.Trigger(AppliedItemBuff.Events.WeaponBuffVFXUpdate, EffectValue(TimeElapsedNormalized));
            }
        }

        protected override void OnExit(bool restarted) {
            base.OnExit(restarted);
            World.EventSystem.TryDisposeListener(ref _mainHandChangedEvent);
            World.EventSystem.TryDisposeListener(ref _offHandChangedEvent);
        }
    }
}