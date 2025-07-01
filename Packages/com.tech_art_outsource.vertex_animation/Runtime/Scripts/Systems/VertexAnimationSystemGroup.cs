using Unity.Entities;

namespace TAO.VertexAnimation {
    [WorldSystemFilter(WorldSystemFilterFlags.Default)]
    [UpdateInGroup(typeof(SimulationSystemGroup), OrderLast = true)]
    public partial class VertexAnimationSystemGroup : ComponentSystemGroup {}
}