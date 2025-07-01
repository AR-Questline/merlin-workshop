using Unity.Entities;

namespace Unity.Rendering
{
    internal struct BlendWeightBufferIndex : IComponentData
    {
        // Keep index 0 reserved as Invalid.
        public const int Null = 0;

        public int Value;
    }
}
