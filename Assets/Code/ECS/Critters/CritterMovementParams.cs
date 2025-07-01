namespace Awaken.ECS.Critters {
    [System.Serializable]
    public struct CritterMovementParams {
        public float rotationSpeed;
        public float movementSpeedMin, movementSpeedMax;
        public float idleTimeMin, idleTimeMax, idleChance;
        
        public CritterMovementParams(float rotationSpeed, float movementSpeedMin, float movementSpeedMax, float idleTimeMin, float idleTimeMax, float idleChance) {
            this.rotationSpeed = rotationSpeed;
            this.movementSpeedMin = movementSpeedMin;
            this.movementSpeedMax = movementSpeedMax;
            this.idleTimeMin = idleTimeMin;
            this.idleTimeMax = idleTimeMax;
            this.idleChance = idleChance;
        }
    }
}