using Unity.Entities;

namespace Unity.Rendering
{
    /// <summary>
    /// A tag component that marks an entity as a custom light probe.
    /// </summary>
    /// <remarks>
    /// The ManageSHPropertiesSystem uses this to manage shadow harmonics.
    /// </remarks>
    public struct CustomProbeTag : IComponentData
    {
    }
}
