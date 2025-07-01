using Unity.Entities;
using Unity.Mathematics;

namespace Unity.Rendering
{
    [MaterialProperty("unity_ProbeVolumeParams")]
    internal struct BuiltinMaterialPropertyUnity_ProbeVolumeParams : IComponentData
    {
        public float4   Value;
    }
}
