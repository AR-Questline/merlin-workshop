using Unity.Entities;

namespace Unity.Rendering
{
    /// <summary>
    /// A tag component that marks an entity as a blend probe.
    /// </summary>
    /// <remarks>
    /// The LightProbeUpdateSystem uses this to manage light probes.
    /// </remarks>
    public struct BlendProbeTag : IComponentData
    {
    }
}
