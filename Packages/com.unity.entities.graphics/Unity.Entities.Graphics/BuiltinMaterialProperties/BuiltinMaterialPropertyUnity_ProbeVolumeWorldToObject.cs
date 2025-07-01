using Unity.Entities;
using Unity.Mathematics;

namespace Unity.Rendering
{
    [MaterialProperty("unity_ProbeVolumeWorldToObject")]
    internal struct BuiltinMaterialPropertyUnity_ProbeVolumeWorldToObject : IComponentData
    {
        public float4x4 Value;
    }
}
