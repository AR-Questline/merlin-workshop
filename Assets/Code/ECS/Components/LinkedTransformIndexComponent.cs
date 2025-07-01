using Unity.Entities;

namespace Awaken.ECS.Components {
    public readonly struct LinkedTransformIndexComponent : ICleanupComponentData {
        public readonly int index;
        
        public LinkedTransformIndexComponent(int index) {
            this.index = index;
        }
    }
}
