using Unity.Entities;
using Unity.Mathematics;

namespace Unity.Rendering
{
    [MaterialProperty("unity_MatrixPreviousM", 4 * 4 * 3)]
    public struct BuiltinMaterialPropertyUnity_MatrixPreviousM : IComponentData
    {
        public float4x4 Value;
    }

    internal struct SkipBuiltinMaterialPropertyUnity_MatrixPreviousMUpdate : IComponentData
    {
    }
}
