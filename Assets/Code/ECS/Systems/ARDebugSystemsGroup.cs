using Unity.Entities;

namespace Awaken.ECS.Systems {
    [WorldSystemFilter(WorldSystemFilterFlags.Default | WorldSystemFilterFlags.Editor)]
    public partial class ARDebugSystemsGroup : ComponentSystemGroup {}
}
