using System;
using Unity.Entities;

namespace Awaken.ECS.Flocks {
    public struct FlyingFlockEntityState : IComponentData {
        public State value;
        
        [Flags]
        public enum State : byte {
            None = 0,
            // States for animation and sound (multi-frame, not overlapping with each other)
            Flapping = 1 << 0,
            Soaring = 1 << 1,
            Resting = 1 << 2,
            // Helper state for tracking transition between landing far and landing close
            LandingFar = 1 << 4,
            // States for sound
            TakingOff = 1 << 5,
            Landing = 1 << 6,
        }
    }
}