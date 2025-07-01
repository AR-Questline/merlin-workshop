#if ENABLE_UNITY_OCCLUSION && (HDRP_10_0_0_OR_NEWER || URP_10_0_0_OR_NEWER)

using Unity.Entities;
using Unity.Mathematics;

namespace Unity.Rendering.Occlusion.Masked.Dots
{
    struct OcclusionTest : IComponentData
    {
        public OcclusionTest(bool enabled)
        {
            this.enabled = enabled;
            screenMin = float.MaxValue;
            screenMax = -float.MaxValue;
        }

        // this flag is for toggling occlusion testing without having to add a component at runtime.
        public bool enabled;
        public float4 screenMin, screenMax;
    }

    struct ChunkOcclusionTest : IComponentData
    {
        public float4 screenMin, screenMax;
    }
}
#endif
