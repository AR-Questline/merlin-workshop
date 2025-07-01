using Unity.Entities;
using Unity.Mathematics;

namespace Unity.Rendering
{
    [MaterialProperty("unity_DynamicLightmapST")]
    internal struct BuiltinMaterialPropertyUnity_DynamicLightmapST : IComponentData
    {
        public float4   Value;
    }
}
