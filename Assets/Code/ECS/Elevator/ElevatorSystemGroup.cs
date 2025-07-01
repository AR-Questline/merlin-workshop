using Unity.Entities;

namespace Awaken.ECS.Elevator {
    [WorldSystemFilter(WorldSystemFilterFlags.Default | WorldSystemFilterFlags.Editor | WorldSystemFilterFlags.ThinClientSimulation)]
    [UpdateInGroup(typeof(SimulationSystemGroup), OrderLast = true)]
    public partial class ElevatorSystemGroup : ComponentSystemGroup {}
}