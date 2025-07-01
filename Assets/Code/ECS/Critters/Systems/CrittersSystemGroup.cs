using Unity.Entities;

namespace Awaken.ECS.Critters {
    [WorldSystemFilter(WorldSystemFilterFlags.Default | WorldSystemFilterFlags.Editor | WorldSystemFilterFlags.ThinClientSimulation)]
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    public partial class CrittersSystemGroup : ComponentSystemGroup { }
}