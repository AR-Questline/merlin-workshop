#if ENABLE_UNITY_OCCLUSION && (HDRP_10_0_0_OR_NEWER || URP_10_0_0_OR_NEWER)

using Unity.Burst;
using Unity.Burst.Intrinsics;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Jobs;

namespace Unity.Rendering.Occlusion.Masked
{
    [BurstCompile]
    unsafe struct ClearJob: IJobFor
    {
        [ReadOnly, NativeDisableUnsafePtrRestriction] public Tile* Tiles;
        public void Execute(int i)
        {
            Tiles[i].zMin0 = new v128(-1f);
            Tiles[i].zMin1 = new v128(0);
            Tiles[i].mask = new v128(0);
        }
    }
}

#endif // ENABLE_UNITY_OCCLUSION && (HDRP_10_0_0_OR_NEWER || URP_10_0_0_OR_NEWER)
