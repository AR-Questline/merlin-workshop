using Unity.Entities;

namespace Awaken.ECS.Flocks {
    public struct FlockRestSpotTimeData : IComponentData {
        public uint currentWaitTimeHash;
        public float waitTimeElapsed;

        public FlockRestSpotTimeData(uint currentWaitTimeHash, float waitTimeElapsed) {
            this.currentWaitTimeHash = currentWaitTimeHash;
            this.waitTimeElapsed = waitTimeElapsed;
        }
    }
}