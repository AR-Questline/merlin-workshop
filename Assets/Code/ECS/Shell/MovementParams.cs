using System;
using Unity.Entities;

namespace Awaken.ECS.Flocks {
    public struct MovementParams : IComponentData {
        public float movementSpeed;
        public float steeringSpeedMult;

        public MovementParams(float movementSpeed, float steeringSpeedMult) {
            throw new NotImplementedException();
        }
    }
}