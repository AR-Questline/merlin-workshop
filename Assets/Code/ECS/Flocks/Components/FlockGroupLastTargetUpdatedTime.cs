using Unity.Entities;

namespace Awaken.ECS.Flocks {
    public struct FlockGroupLastTargetUpdatedTime : IComponentData {
        public float value;

        public FlockGroupLastTargetUpdatedTime(float value) {
            this.value = value;
        }
    }
}