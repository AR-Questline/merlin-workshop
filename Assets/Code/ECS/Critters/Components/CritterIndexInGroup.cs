using Unity.Entities;

namespace Awaken.ECS.Critters.Components {
    public struct CritterIndexInGroup : IComponentData {
        public int value;
        public CritterIndexInGroup(int value) {
            this.value = value;
        }
    }
}