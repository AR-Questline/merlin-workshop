using Unity.Entities;

namespace Awaken.ECS.Flocks {
    public struct ReachDistanceToTarget : IComponentData {
        public float reachDistance;
        public ReachDistanceToTarget(float reachDistance) {
            this.reachDistance = reachDistance;
        }
    }
}