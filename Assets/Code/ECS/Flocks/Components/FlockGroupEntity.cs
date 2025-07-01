using Unity.Entities;

namespace Awaken.ECS.Flocks {
    public struct FlockGroupEntity : ISharedComponentData {
        public Entity value;

        public FlockGroupEntity(Entity value) {
            this.value = value;
        }

        public static implicit operator Entity(FlockGroupEntity value) => value.value;
    }
}