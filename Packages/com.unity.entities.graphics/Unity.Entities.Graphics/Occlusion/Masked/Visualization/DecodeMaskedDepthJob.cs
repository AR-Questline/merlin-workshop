#if ENABLE_UNITY_OCCLUSION && (HDRP_10_0_0_OR_NEWER || URP_10_0_0_OR_NEWER)

using Unity.Burst;
using Unity.Burst.Intrinsics;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Jobs;

namespace Unity.Rendering.Occlusion.Masked.Visualization
{
    /* Unpack the CPU-rasterized masked depth buffer into a human-readable depth buffer. */
    [BurstCompile]
    unsafe struct DecodeMaskedDepthJob : IJobParallelFor
    {
        [ReadOnly] public int NumPixelsX;
        [ReadOnly] public int NumPixelsY;
        [ReadOnly] public int NumTilesX;
        [ReadOnly, NativeDisableUnsafePtrRestriction] public Tile* Tiles;

        [WriteOnly] public NativeArray<float> DecodedZBuffer;

        // i => tile index
        public void Execute(int i)
        {
            float* zBuffer = (float*)DecodedZBuffer.GetUnsafePtr();

            // this is a 32x4 tile
            var tile = Tiles[i];

            int numTilesX = NumPixelsX / BufferGroup.TileWidth;
            int numTilesY = NumPixelsY / BufferGroup.TileHeight;

            int tx = i % numTilesX;
            int ty = i / numTilesX;

            // iterate over the four 8x4 subtiles
            for (int j = 0; j < 4; j++)
            {
                // prepare two vectors of zMin0 and zMin1
                // splat j's element
                var subTilez0 = new v128(IntrinsicUtils.getFloatLane(tile.zMin0, (uint)j));
                var subTilez1 = new v128(IntrinsicUtils.getFloatLane(tile.zMin1, (uint)j));

                var testMask = new v128(1, 2, 4, 8);

                // the mask is 32 bit, 8x4 bits
                // iterate over each byte
                for (int k = 0; k < 4; k++)
                {
                    // extract mask for the subtile
                    byte subTileMask = IntrinsicUtils.getByteLane(tile.mask, (uint)(j * 4 + k));

                    // now, make low and high half-bytes into a int32x4 mask for blending
                    // high
                    int highHalfByte = subTileMask >> 4;
                    var highMask = new v128(highHalfByte);
                    // low
                    int lowHalfByte = subTileMask & 15;
                    var lowMask = new v128(lowHalfByte);

                    if (Arm.Neon.IsNeonSupported)
                    {
                        var blendMaskHigh = Arm.Neon.vtstq_s32(highMask, testMask);
                        var zResultHigh = Arm.Neon.vbslq_s8(blendMaskHigh, subTilez1, subTilez0);

                        var blendMaskLow = Arm.Neon.vtstq_s32(lowMask, testMask);
                        var zResultLow = Arm.Neon.vbslq_s8(blendMaskLow, subTilez1, subTilez0);

                        int index = ((NumPixelsY - (BufferGroup.TileHeight * ty + k)) * NumPixelsX + BufferGroup.TileWidth * tx + BufferGroup.SubTileWidth * j);

                        // save to DecodedZBuffer
                        // this generates STP which is most efficient
                        Arm.Neon.vst1q_f32(zBuffer + index, zResultLow);
                        Arm.Neon.vst1q_f32(zBuffer + index + 4, zResultHigh);
                    }
                    else if (X86.Sse4_1.IsSse41Supported)
                    {
                        var invBlendMaskHigh = X86.Sse2.cmpeq_epi32(X86.Sse2.and_si128(highMask, testMask), X86.Sse2.setzero_si128());
                        var zResultHigh = X86.Sse4_1.blendv_ps(subTilez1, subTilez0, invBlendMaskHigh);

                        var invBlendMaskLow = X86.Sse2.cmpeq_epi32(X86.Sse2.and_si128(lowMask, testMask), X86.Sse2.setzero_si128());
                        var zResultLow = X86.Sse4_1.blendv_ps(subTilez1, subTilez0, invBlendMaskLow);

                        int index = ((NumPixelsY - (BufferGroup.TileHeight * ty + k)) * NumPixelsX + BufferGroup.TileWidth * tx + BufferGroup.SubTileWidth * j);

                        v128* zBufferSimd = (v128*)zBuffer;
                        zBufferSimd[index / 4] = zResultLow;
                        zBufferSimd[index / 4 + 1] = zResultHigh;
                    }
                }
            }
        }
    }
}

#endif // ENABLE_UNITY_OCCLUSION && (HDRP_10_0_0_OR_NEWER || URP_10_0_0_OR_NEWER)
