#if ENABLE_UNITY_OCCLUSION && (HDRP_10_0_0_OR_NEWER || URP_10_0_0_OR_NEWER)

using Unity.Burst.Intrinsics;

namespace Unity.Rendering.Occlusion.Masked
{
    enum CullingResult
    {
        VISIBLE = 0x0,
        OCCLUDED = 0x1,
        VIEW_CULLED = 0x3
    }
    
    struct Tile
    {
        public v128 zMin0;
        public v128 zMin1;
        public v128 mask;
    }
    
    struct ScissorRect
    {
        public int mMinX; //!< Screen space X coordinate for left side of scissor rect, inclusive and must be a multiple of 32
        public int mMinY; //!< Screen space Y coordinate for bottom side of scissor rect, inclusive and must be a multiple of 8
        public int mMaxX; //!< Screen space X coordinate for right side of scissor rect, <B>non</B> inclusive and must be a multiple of 32
        public int mMaxY; //!< Screen space Y coordinate for top side of scissor rect, <B>non</B> inclusive and must be a multiple of 8
    }
}
#endif
