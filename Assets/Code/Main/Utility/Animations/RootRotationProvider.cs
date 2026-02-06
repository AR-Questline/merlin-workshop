using System;
using Animancer;
using Awaken.TG.Main.Utility.Animations.ARTransitions;

namespace Awaken.TG.Main.Utility.Animations {
    public class RootRotationProvider : IDisposable {
        public ARClipTransition Transition { get; private set; }
        public AnimancerState State { get; private set; }
        
        public bool IsActive => State is { IsValid: true, IsPlaying: true };
        public float TargetRootRotation => Transition.TargetRootRotation;
        
        public RootRotationProvider(ARClipTransition transition, AnimancerState state) {
            Transition = transition;
            State = state;
        }
        
        public float GetRootRotationDelta(float deltaTime) {
            if (!State.Root.IsGraphPlaying) {
                return 0f;
            }
            
            float scaledDeltaTime = deltaTime * State.Speed;
            float beginSampleTime = State.Time - scaledDeltaTime;
            return Transition.GetRootRotationDelta(beginSampleTime, scaledDeltaTime);
        }

        public void Dispose() {
            Transition = null;
            State = null;
        }
    }
}