using Unity.Entities;

namespace Unity.Rendering
{
    [MaterialProperty("_SkinMatrixIndex")]
    internal struct SkinMatrixBufferIndex : IComponentData
    {
        // Keep index 0 reserved as Invalid.
        public const int Null = 0;

        public int Value;
    }
}
