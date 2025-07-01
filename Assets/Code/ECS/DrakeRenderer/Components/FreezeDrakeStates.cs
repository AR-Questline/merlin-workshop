using Unity.Entities;

namespace Awaken.ECS.DrakeRenderer.Components {
    public struct FreezeDrakeStates : IComponentData {
        public byte counter;
    }
}
