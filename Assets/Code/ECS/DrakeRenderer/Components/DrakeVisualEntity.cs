using Unity.Entities;

namespace Awaken.ECS.DrakeRenderer {
    [InternalBufferCapacity(3)]
    public struct DrakeVisualEntity : IBufferElementData {
        public Entity value;

        public DrakeVisualEntity(Entity value) {
            this.value = value;
        }

        public static implicit operator Entity(DrakeVisualEntity value) => value.value;
    }
}