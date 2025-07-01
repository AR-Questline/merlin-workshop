using UnityEngine;

namespace Pathfinding {
    public interface IDeltaPositionLimiter {
        public Vector2 LimitDeltaPosition(Vector3 currentPosition, Vector2 currentDeltaPosition);
    }
    
    public class DefaultDeltaPositionLimiter : IDeltaPositionLimiter {
        public Vector2 LimitDeltaPosition(Vector3 currentPosition, Vector2 currentDeltaPosition) => currentDeltaPosition;
    }
}