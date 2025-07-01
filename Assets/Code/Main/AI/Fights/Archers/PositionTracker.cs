using UnityEngine;

namespace Awaken.TG.Main.AI.Fights.Archers {
    public class PositionTracker {
        Vector3 _position;
        Vector3 _velocity;
        Vector3 _acceleration;

        [UnityEngine.Scripting.Preserve]
        public void SetNewPositionAfterTime(Vector3 newPosition, float deltaTime) {
            float inverseDeltaTime = 1 / deltaTime;
            Vector3 newVelocity = (newPosition - _position) * inverseDeltaTime;
            _acceleration = (newVelocity - _velocity) * inverseDeltaTime;
            _velocity = newVelocity;
            _position = newPosition;
        }
        
        [UnityEngine.Scripting.Preserve]
        public Vector3 PositionOffsetInTime(float time) {
            return (time * 0.5f * _acceleration + _velocity) * time;
        }
    }
}