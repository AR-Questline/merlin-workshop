using Unity.Entities;

namespace Awaken.ECS.Flocks {
    public struct FlockRestSpotStatus : IComponentData {
        public Entity restingEntity;
        public float blockChangesUntilTime;

        public bool HasEntity => restingEntity.Equals(Entity.Null) == false;
    }
}