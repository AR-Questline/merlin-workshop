using Unity.Entities;
using Unity.Mathematics;

namespace Unity.Rendering
{
    [MaterialProperty("unity_LightmapIndex")]
    internal struct BuiltinMaterialPropertyUnity_LightmapIndex : IComponentData
    {
        public float4   Value;
    }
}
