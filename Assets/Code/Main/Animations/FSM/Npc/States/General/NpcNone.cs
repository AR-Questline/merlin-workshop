using Awaken.Utility;
using System;
using Animancer;
using Awaken.TG.Main.Animations.FSM.Npc.Base;
using UnityEngine;

namespace Awaken.TG.Main.Animations.FSM.Npc.States.General {
    public sealed partial class NpcNone : NpcAnimatorState {
        public override ushort TypeForSerialization => SavedModels.NpcNone;

        const int DefaultTransitionSpeed = 2;
        
        float _transitionSpeed;
        float _currentWeight;
        bool _stopped;
        
        public override NpcStateType Type => NpcStateType.None;

        public override void Enter(float _, float? overrideCrossFadeTime, Action<ITransition> onNodeLoaded = null) {
            Entered = true;
            _currentWeight = AnimancerLayer.Weight;
            
            _transitionSpeed = DefaultTransitionSpeed;
            if (overrideCrossFadeTime.HasValue) {
                float value = overrideCrossFadeTime.Value;
                _transitionSpeed = value > 0 ? 1 / value : 0;
            }
            
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
                _currentWeight = Mathf.Max(0, _currentWeight - (deltaTime * _transitionSpeed));
                AnimancerLayer.Weight = _currentWeight;
            } else {
                Stop();
            }
        }

        void Stop() {
            AnimancerLayer.Weight = 0;
            AnimancerLayer.Stop();
            _stopped = true;
        }
    }
}