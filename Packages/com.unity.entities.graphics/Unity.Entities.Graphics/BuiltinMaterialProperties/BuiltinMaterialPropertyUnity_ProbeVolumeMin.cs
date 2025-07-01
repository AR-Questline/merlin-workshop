using Unity.Entities;
using Unity.Mathematics;

namespace Unity.Rendering
{
    [MaterialProperty("unity_ProbeVolumeMin")]
    internal struct BuiltinMaterialPropertyUnity_ProbeVolumeMin : IComponentData
    {
        public float4   Value;
    }
}
