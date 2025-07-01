using Unity.Entities;

namespace Unity.Rendering
{
    /// <summary>
    /// Represents a system group that contains systems that perform structural changes.
    /// </summary>
    /// <remarks>
    /// Any system that makes structural changes must be in this system group. Structural changes performed after can result in undefined behavior
    /// or even crashing the application.
    /// </remarks>
    [UpdateInGroup(typeof(PresentationSystemGroup))]
    [WorldSystemFilter(WorldSystemFilterFlags.Default | WorldSystemFilterFlags.EntitySceneOptimizations | WorldSystemFilterFlags.Editor)]
    public partial class StructuralChangePresentationSystemGroup : ComponentSystemGroup
    {
    }
}
