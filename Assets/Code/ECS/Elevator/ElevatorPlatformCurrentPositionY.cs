using Unity.Entities;

namespace Awaken.ECS.Elevator {
    public struct ElevatorPlatformCurrentPositionY : IComponentData {
        public float value;
        public ElevatorPlatformCurrentPositionY(float value) {
            this.value = value;
        }
    }
}