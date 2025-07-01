using Unity.Entities;
using Unity.Mathematics;

namespace Unity.Rendering
{
    [MaterialProperty("unity_ProbeVolumeSizeInv")]
    internal struct BuiltinMaterialPropertyUnity_ProbeVolumeSizeInv : IComponentData
    {
        public float4   Value;
    }
}
