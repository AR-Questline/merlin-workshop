using Animancer;
using Awaken.TG.Main.Animations.FSM.Heroes.Base;
using Awaken.TG.Main.Animations.FSM.Heroes.States.TPP;
using Awaken.TG.Main.Utility.Animations;
using UnityEngine;

namespace Awaken.TG.Main.Animations.FSM.Heroes.States.Shared {
    public partial class MovementState : MovementState<HeroAnimatorSubstateMachine> { }
    
    public abstract partial class MovementState<T> : HeroAnimatorState<T>, ISynchronizedAnimatorState where T : HeroAnimatorSubstateMachine {
        protected MixerState<Vector2> _mixerState;
        protected HeroStateType _lastMovementState;
        
        public override HeroGeneralStateType GeneralType => HeroGeneralStateType.General;
        public override HeroStateType Type => HeroStateType.Movement;
        public override HeroStateType StateToEnter => UseAlternateState ? HeroStateType.MovementAlternate : HeroStateType.Movement;
        public override bool CanReEnter => true;
        protected override bool HeadBobbingDependent => true;
        protected virtual bool ExitToIdleCondition => HeroAnimancer.MovementSpeed < 0.05f || !Hero.Grounded;
        protected override float OffsetNormalizedTime(float previousNormalizedTime) => ParentModel.SynchronizedStateOffsetNormalizedTime();
        protected float BlendSpeed => AnimancerUtils.BlendTreeBlendSpeed();
        protected virtual float? ReEnterBlendDuration => null;

        protected override void AfterEnter(float previousStateNormalizedTime) {
            _mixerState = (MixerState<Vector2>)CurrentState;
            if (_mixerState != null) {
                _mixerState.Parameter = Vector2.zero;
            }
            _lastMovementState = StateToEnter;
        }

        protected override void OnUpdate(float deltaTime) {
            if (ExitToIdleCondition) {
                ParentModel.SetCurrentState(HeroStateType.Idle);
                return;
            }

            UpdateMixerParameter(deltaTime);

            if (_lastMovementState != StateToEnter) {
                ParentModel.SetCurrentState(Type, ReEnterBlendDuration);
            }
        }

        protected virtual void UpdateMixerParameter(float deltaTime) {
            if (_mixerState != null) {
                Vector2 mixerParam;
                if (Main.Heroes.Hero.TppActive) {
                    mixerParam = new Vector2(Hero.RelativeVelocity.y, Hero.RelativeVelocity.x);
                } else {
                    mixerParam = new Vector2(0, HeroAnimancer.MovementSpeed);
                }
                _mixerState.Parameter = Vector2.MoveTowards(_mixerState.Parameter, mixerParam, BlendSpeed * deltaTime);
            }
        }
    }
}