using Unity.Entities;
using Unity.Mathematics;

namespace Unity.Rendering
{
    [MaterialProperty("unity_LODFade")]
    internal struct BuiltinMaterialPropertyUnity_LODFade : IComponentData
    {
        public float4   Value;
    }
}
