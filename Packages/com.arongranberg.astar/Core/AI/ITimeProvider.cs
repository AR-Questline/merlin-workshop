using UnityEngine;

namespace Pathfinding {
    public interface ITimeProvider {
        bool IsValid { get; }
        float GetDeltaTime();
        float GetFixedDeltaTime();
    }

    public class UnityTimeProvider : ITimeProvider {
        public bool IsValid => true;
        public float GetDeltaTime() => Time.deltaTime;
        public float GetFixedDeltaTime() => Time.fixedDeltaTime;
    }
}