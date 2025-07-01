using System;
using Animancer;
using Awaken.TG.Main.Animations.FSM.Heroes.Base;
using Unity.Mathematics;

namespace Awaken.TG.Main.Animations.FSM.Heroes.States.Overrides {
    public partial class HeroNoneState : HeroAnimatorState {
        public const int TransitionSpeed = 2;

        public override HeroGeneralStateType GeneralType => HeroGeneralStateType.General;
        public override HeroStateType Type => HeroStateType.None;

        float _transitionSpeed;
        float _currentWeight;
        bool _stopped;
        
        public override void Enter(float _, float? overrideCrossFadeTime, Action<ITransition> onNodeLoaded = null) {
            Entered = true;
            _currentWeight = ParentModel.AnimancerLayer.Weight;

            _transitionSpeed = GetTransitionSpeed(overrideCrossFadeTime);
            
            _stopped = false;

            if (_transitionSpeed == 0) {
                Stop();
            }
            
            onNodeLoaded?.Invoke(null);
        }

        protected override void OnUpdate(float deltaTime) {
            if (_stopped) {
                return;
            }
            
            if (_currentWeight > 0) {
                _currentWeight = math.max(0, _currentWeight - (deltaTime * _transitionSpeed));
                ParentModel.AnimancerLayer.Weight = _currentWeight;
            } else {
                Stop();
            }
        }

        void Stop() {
            ParentModel.AnimancerLayer.Weight = 0;
            ParentModel.AnimancerLayer.Stop();
            _stopped = true;
        }

        public static float GetTransitionSpeed(float? overrideCrossFadeTime) {
            float transitionSpeed = TransitionSpeed;
            if (overrideCrossFadeTime.HasValue) {
                float value = overrideCrossFadeTime.Value;
                transitionSpeed = value > 0 ? 1 / value : 0;
            }
            return transitionSpeed;
        }
    }
}