using Awaken.TG.Main.Animations.FSM.Heroes.Base;
using Awaken.TG.Main.Animations.FSM.Heroes.Machines;
using Awaken.TG.Main.Fights.Finishers;
using Awaken.TG.Main.Heroes;
using Awaken.TG.Main.Heroes.Combat;
using Awaken.TG.Main.Heroes.MovementSystems;
using Awaken.TG.Main.Timing;
using Awaken.TG.Main.Timing.ARTime;
using Awaken.TG.Main.Timing.ARTime.Modifiers;
using Awaken.TG.Main.Utility.Animations;
using Awaken.TG.MVC;
using Awaken.TG.MVC.Events;
using UnityEngine;

namespace Awaken.TG.Main.Animations.FSM.Heroes.States.Overrides {
    public partial class FinisherState : HeroAnimatorState {
        const string SlowdownID = "Finisher";
        const float WorldTimeScale = 0.02f;
        const float ReverseTimeScale = 1 / WorldTimeScale;
        const float SlowdownDisableOnRemainingTime = 0.5f;

        FinisherData.RuntimeData _data;
        ITimeModifier[] _slowDowns;

        public override HeroGeneralStateType GeneralType => HeroGeneralStateType.Finisher;
        public override HeroStateType Type => HeroStateType.Finisher;

        public new static class Events {
            public static readonly Event<Hero, FinisherData.RuntimeData> FinisherStarted = new(nameof(FinisherStarted));
            public static readonly Event<Hero, bool> FinisherEnded = new(nameof(FinisherEnded));
            public static readonly Event<Hero, ARFinisherEffectsData> FinisherAnimationEvent = new(nameof(FinisherAnimationEvent));
        }

        protected override void OnInitialize() {
            Hero.ListenTo(Events.FinisherStarted, OnFinisherStarted, this);
            Hero.ListenTo(Events.FinisherAnimationEvent, OnFinisherAnimationEvent, this);
            base.OnInitialize();
        }

        void OnFinisherStarted(FinisherData.RuntimeData data) {
            _data = data;
            
            ParentModel.SetCurrentState(HeroStateType.Finisher, 0.1f);
            if (Hero.TrySetMovementType(out FinisherMovement movement)) {
                movement.Setup(data);
            }

            if (data.slowDownTime) {
                var globalTime = World.Only<GlobalTime>();
                _slowDowns = new ITimeModifier[] {
                    new DirectTimeMultiplier(WorldTimeScale, SlowdownID),
                    new DirectTimeMultiplier(ReverseTimeScale, SlowdownID),
                    new DirectTimeMultiplier(ReverseTimeScale, SlowdownID),
                };
                globalTime.AddTimeModifier(_slowDowns[0]);
                Hero.AddTimeModifier(_slowDowns[1]);
                data.target.AddTimeModifier(_slowDowns[2]);
            }
        }

        protected override void AfterEnter(float previousStateNormalizedTime) {
            Hero.VHeroController.HeroCamera.SetPitch(0);
        }

        void OnFinisherAnimationEvent(ARFinisherEffectsData data) {
            data.Play(_data.target, Hero);
        }

        protected override void OnUpdate(float deltaTime) {
            if (CurrentState.RemainingDuration / Time.timeScale <= SlowdownDisableOnRemainingTime) {
                RemoveSlowdowns();
            }

            if (TimeElapsedNormalized >= 0.99f) {
                ParentModel.SetCurrentState(ParentModel is LegsFSM ? HeroStateType.Idle : HeroStateType.None, 0f);
            }
        }

        protected override void OnExit(bool restarted) {
            Hero.Current.ReturnToDefaultMovement();
            RemoveSlowdowns();
            Hero.Trigger(Events.FinisherEnded, true);
        }

        void RemoveSlowdowns() {
            if (_slowDowns != null) {
                foreach (var slowDown in _slowDowns) {
                    slowDown.Remove();
                }
                _slowDowns = null;
            }
        }
    }
}