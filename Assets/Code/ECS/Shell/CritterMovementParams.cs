using System;

namespace Awaken.ECS.Critters {
    public struct CritterMovementParams {
        public float rotationSpeed;
        public float movementSpeedMin, movementSpeedMax;
        public float idleTimeMin, idleTimeMax, idleChance;

        public CritterMovementParams(float rotationSpeed, float movementSpeedMin, float movementSpeedMax,
            float idleTimeMin, float idleTimeMax, float idleChance) {
            throw new NotImplementedException();
        }
    }
}