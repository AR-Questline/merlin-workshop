using Unity.Entities;

namespace Awaken.ECS.Flocks {
    [WorldSystemFilter(WorldSystemFilterFlags.Default | WorldSystemFilterFlags.Editor | WorldSystemFilterFlags.ThinClientSimulation)]
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    public partial class FlockSystemGroup : ComponentSystemGroup {}
}