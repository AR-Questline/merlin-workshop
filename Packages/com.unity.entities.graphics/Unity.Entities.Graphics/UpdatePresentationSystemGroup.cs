using Unity.Entities;

namespace Unity.Rendering
{
    /// <summary>
    /// Represents a system group that is used to establish the order of execution of the other systems.
    /// </summary>
    [UpdateInGroup(typeof(PresentationSystemGroup))]
    [UpdateAfter(typeof(StructuralChangePresentationSystemGroup))]
    [WorldSystemFilter(WorldSystemFilterFlags.Default | WorldSystemFilterFlags.EntitySceneOptimizations | WorldSystemFilterFlags.Editor)]
    public partial class UpdatePresentationSystemGroup : ComponentSystemGroup
    {
    }
}
