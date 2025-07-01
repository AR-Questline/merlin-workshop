#if ENABLE_UNITY_OCCLUSION && (HDRP_10_0_0_OR_NEWER || URP_10_0_0_OR_NEWER)

using System.Runtime.CompilerServices;
using Unity.Burst;
using Unity.Burst.Intrinsics;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine.Rendering;
using Unity.Rendering.Occlusion.Masked.Dots;

namespace Unity.Rendering.Occlusion.Masked
{
    [BurstCompile]
    unsafe struct TestJob : IJobParallelForDefer
    {
        public IndirectList<ChunkVisibilityItem> VisibilityItems;
        [ReadOnly] public ComponentTypeHandle<ChunkHeader> ChunkHeader;
        [ReadOnly] public ComponentTypeHandle<OcclusionTest> OcclusionTest;
        [ReadOnly] public ComponentTypeHandle<ChunkOcclusionTest> ChunkOcclusionTest;
        [ReadOnly] public ComponentTypeHandle<EntitiesGraphicsChunkInfo> EntitiesGraphicsChunkInfo;
        [ReadOnly] public BatchCullingProjectionType ProjectionType;
        [ReadOnly] public int NumTilesX;
        [ReadOnly] public v128 HalfSize;
        [ReadOnly] public v128 PixelCenter;
        [ReadOnly] public v128 ScreenSize;
        [ReadOnly] public BatchCullingViewType ViewType;
        [ReadOnly] public int SplitIndex;
        [ReadOnly, NativeDisableUnsafePtrRestriction] public Tile* Tiles;
        [ReadOnly] public bool DisplayOnlyOccluded;

        public void Execute(int index)
        {
            var visibilityItem = VisibilityItems.ElementAt(index);
            var chunk = visibilityItem.Chunk;
            var chunkVisibility = visibilityItem.Visibility;

            if (!chunk.HasChunkComponent(ref ChunkOcclusionTest))
            {
                /* Because this is not a IJobChunk job, it will not filter by archetype. So there is no guarantee that
                   the current chunk has any occlusion test jobs on it. */
                return;
            }

            var hybridChunkInfo = chunk.GetChunkComponentData(ref EntitiesGraphicsChunkInfo);
            if (!hybridChunkInfo.Valid)
                return;

            var anyLodEnabled = (
                hybridChunkInfo.CullingData.InstanceLodEnableds.Enabled[0] |
                hybridChunkInfo.CullingData.InstanceLodEnableds.Enabled[1]
            ) != 0;

            if (!anyLodEnabled)
            {
                /* Cull the whole chunk if no LODs are enabled */
                chunkVisibility->VisibleEntities[0] = 0;
                chunkVisibility->VisibleEntities[1] = 0;
                return;
            }

            var chunkTest = chunk.GetChunkComponentData(ref ChunkOcclusionTest);
            CullingResult chunkCullingResult = TestRect(
                chunkTest.screenMin.xy,
                chunkTest.screenMax.xy,
                chunkTest.screenMin.w,
                Tiles,
                ProjectionType,
                NumTilesX,
                ScreenSize,
                HalfSize,
                PixelCenter
            );

            bool chunkVisible = (chunkCullingResult == CullingResult.VISIBLE);
            /* If we want to invert occlusion for debug purposes, we want to draw _only_ occluded entities. For this, we
               want to run occlusion on every chunk, regardless of that chunk's test. A clearer but branch-ey way to
               write this is:

               if (DisplayOnlyOccluded) {
                   chunkVisible = true;
               } */
            chunkVisible |= DisplayOnlyOccluded;
            if (!chunkVisible)
            {
                /* The chunk's bounding box fails the visibility test, which means that it's either frustum culled or
                   occlusion culled. Cull the whole chunk and early out. */
                if (ViewType != BatchCullingViewType.Light)
                {
                    // When culling light views, we don't zero out the visible entities
                    // because the entity might be visible in a different split. This is
                    // not the case for other view types, so we must zero them out here.
                    chunkVisibility->VisibleEntities[0] = 0;
                    chunkVisibility->VisibleEntities[1] = 0;
                }
                return;
            }

            var tests = chunk.GetNativeArray(ref OcclusionTest);

            /* Each chunk is guaranteed to have no more than 128 entities. So the Entities Graphics package uses `VisibleEntities`,
               which is an array of two 64-bit integers to indicate whether each of these entities is visible. */
            for (int j = 0; j < 2; j++)
            {
                /* The pending bitfield indicates which incoming entities are to be occlusion-tested.
                   - If a bit is zero, the corresponding entity is already not drawn by a previously run system; e.g. it
                   might be frustum culled. So there's no need to process it further.
                   - If a bit is one, the corresponding entity needs to be occlusion-tested. */
                var pendingBitfield = chunkVisibility->VisibleEntities[j];
                ulong newBitfield = 0;

                /* Once the whole pending bitfield is zero, we don't need to do any more occlusion tests */
                while (pendingBitfield != 0)
                {
                    /* Get the index of the first visible entity using tzcnt. For example:

                       pendingBitfield = ...0000 0000 0000 1010 0000
                                         ▲                   ▲  ▲
                                         │                   │  │
                                         `leading zeros      │  `trailing zeros
                                                             `tzcount = 5

                       Then add (j << 6) to it, which adds 64 if we're in the second bitfield, i.e. if we're covering
                       entities [65, 128].
                    */
                    var tzIndex = math.tzcnt(pendingBitfield);
                    var entityIndex = (j << 6) + tzIndex;

                    /* If the view type is a light, then we check to see whether the current entity is already culled
                       in the current split. If the view type is not a light, we ignore the split mask and proceed to
                       occlusion cull. */
                    bool entityAlreadyFrustumCulled =
                        ViewType == BatchCullingViewType.Light &&
                        ((chunkVisibility->SplitMasks[entityIndex] & (1 << SplitIndex)) == 0);

                    bool entityVisible = false;
                    if (!entityAlreadyFrustumCulled)
                    {
                        CullingResult result = TestRect(
                            tests[entityIndex].screenMin.xy,
                            tests[entityIndex].screenMax.xy,
                            tests[entityIndex].screenMin.w,
                            Tiles,
                            ProjectionType,
                            NumTilesX,
                            ScreenSize,
                            HalfSize,
                            PixelCenter
                        );

                        entityVisible = (result == CullingResult.VISIBLE);
                    }

                    /* This effectively XORs the two booleans, and only flips visible when the inversion boolean is true. A
                       clearer but branch-ey way to write this is:

                       if (DisplayOnlyOccluded) {
                           entityVisible = !entityVisible;
                       } */
                    entityVisible = (entityVisible != DisplayOnlyOccluded);
                    /* Set the index we just processed to zero, indicating that it's not pending any more */
                    pendingBitfield ^= 1ul << tzIndex;
                    /* Set entity's visibility according to our occlusion test */
                    newBitfield |= (entityVisible ? 1UL : 0) << tzIndex;

                    if (!entityAlreadyFrustumCulled && !entityVisible)
                    {
                        /* Set the current split's bit to zero */
                        chunkVisibility->SplitMasks[entityIndex] &= (byte)~(1 << SplitIndex);
                    }
                }

                if (ViewType != BatchCullingViewType.Light)
                {
                    chunkVisibility->VisibleEntities[j] = newBitfield;
                }
                else
                {
                    /* TODO: This will incur a bit of extra work later down the line in case of lights. It won't be too
                       much work because the splitMasks will indicate whether any split contains the entity.
                       This code will change once we handle all splits in the same job */
                    chunkVisibility->VisibleEntities[j] |= newBitfield;
                }
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static CullingResult TestRectSSE(
            float2 min,
            float2 max,
            float wmin,
            Tile* tiles,
            BatchCullingProjectionType projectionType,
            int NumTilesX,
            v128 ScreenSize,
            v128 HalfSize,
            v128 PixelCenter
        )
        {
            if (X86.Sse4_1.IsSse41Supported)
            {
                // Compute screen space bounding box and guard for out of bounds
                v128 pixelBBox = IntrinsicUtils._mmw_fmadd_ps(X86.Sse.setr_ps(min.x, max.x, max.y, min.y), HalfSize, PixelCenter);
                v128 pixelBBoxi = X86.Sse2.cvttps_epi32(pixelBBox);
                pixelBBoxi = X86.Sse4_1.max_epi32(X86.Sse2.setzero_si128(), X86.Sse4_1.min_epi32(ScreenSize, pixelBBoxi));

                // Pad bounding box to (32xN) tiles. Tile BB is used for looping / traversal
                v128 SimdTilePad = X86.Sse2.setr_epi32(0, BufferGroup.TileWidth, 0, BufferGroup.TileHeight);
                v128 SimdTilePadMask = X86.Sse2.setr_epi32(
                    ~(BufferGroup.TileWidth - 1),
                    ~(BufferGroup.TileWidth - 1),
                    ~(BufferGroup.TileHeight - 1),
                    ~(BufferGroup.TileHeight - 1)
                );
                v128 tileBBoxi = X86.Sse2.and_si128(X86.Sse2.add_epi32(pixelBBoxi, SimdTilePad), SimdTilePadMask);

                int txMin = tileBBoxi.SInt0 >> BufferGroup.TileWidthShift;
                int txMax = tileBBoxi.SInt1 >> BufferGroup.TileWidthShift;
                int tileRowIdx = (tileBBoxi.SInt2 >> BufferGroup.TileHeightShift) * NumTilesX;
                int tileRowIdxEnd = (tileBBoxi.SInt3 >> BufferGroup.TileHeightShift) * NumTilesX;

                // Pad bounding box to (8x4) subtiles. Skip SIMD lanes outside the subtile BB
                v128 SimdSubTilePad = X86.Sse2.setr_epi32(0, BufferGroup.SubTileWidth, 0, BufferGroup.SubTileHeight);
                v128 SimdSubTilePadMask = X86.Sse2.setr_epi32(
                    ~(BufferGroup.SubTileWidth - 1),
                    ~(BufferGroup.SubTileWidth - 1),
                    ~(BufferGroup.SubTileHeight - 1),
                    ~(BufferGroup.SubTileHeight - 1)
                );
                v128 subTileBBoxi = X86.Sse2.and_si128(X86.Sse2.add_epi32(pixelBBoxi, SimdSubTilePad), SimdSubTilePadMask);

                v128 stxmin = X86.Sse2.set1_epi32(subTileBBoxi.SInt0 - 1); // - 1 to be able to use GT test
                v128 stymin = X86.Sse2.set1_epi32(subTileBBoxi.SInt2 - 1); // - 1 to be able to use GT test
                v128 stxmax = X86.Sse2.set1_epi32(subTileBBoxi.SInt1);
                v128 stymax = X86.Sse2.set1_epi32(subTileBBoxi.SInt3);

                // Setup pixel coordinates used to discard lanes outside subtile BB
                v128 SimdSubTileColOffset = X86.Sse2.setr_epi32(
                    0,
                    BufferGroup.SubTileWidth,
                    BufferGroup.SubTileWidth * 2,
                    BufferGroup.SubTileWidth * 3
                );
                v128 startPixelX = X86.Sse2.add_epi32(SimdSubTileColOffset, X86.Sse2.set1_epi32(tileBBoxi.SInt0));
                // TODO: (Apoorva) LHS is zero. We can just use the RHS directly.
                v128 pixelY = X86.Sse2.add_epi32(X86.Sse2.setzero_si128(), X86.Sse2.set1_epi32(tileBBoxi.SInt2));

                // Compute z from w. Note that z is reversed order, 0 = far, 1/near = near, which
                // means we use a greater than test, so zMax is used to test for visibility. (z goes from 0 = far to 2 = near for ortho)

                v128 zMax;
                if (projectionType == BatchCullingProjectionType.Orthographic)
                {
                    zMax = IntrinsicUtils._mmw_fmadd_ps(X86.Sse.set1_ps(-1.0f), X86.Sse.set1_ps(wmin), X86.Sse.set1_ps(1.0f));
                }
                else
                {
                    zMax = X86.Sse.div_ps(X86.Sse.set1_ps(1f), X86.Sse.set1_ps(wmin));
                }

                for (; tileRowIdx < tileRowIdxEnd; tileRowIdx += NumTilesX)
                {
                    v128 pixelX = startPixelX;

                    for (int tx = txMin; tx < txMax; tx++)
                    {
                        int tileIdx = tileRowIdx + tx;

                        // Fetch zMin from masked hierarchical Z buffer
                        v128 mask = tiles[tileIdx].mask;
                        v128 zMin0 = X86.Sse4_1.blendv_ps(tiles[tileIdx].zMin0, tiles[tileIdx].zMin1, X86.Sse2.cmpeq_epi32(mask, X86.Sse2.set1_epi32(~0)));
                        v128 zMin1 = X86.Sse4_1.blendv_ps(tiles[tileIdx].zMin1, tiles[tileIdx].zMin0, X86.Sse2.cmpeq_epi32(mask, X86.Sse2.setzero_si128()));
                        v128 zBuf = X86.Sse.min_ps(zMin0, zMin1);

                        // Perform conservative greater than test against hierarchical Z buffer (zMax >= zBuf means the subtile is visible)
                        v128 zPass = X86.Sse.cmpge_ps(zMax, zBuf);  //zPass = zMax >= zBuf ? ~0 : 0

                        // Mask out lanes corresponding to subtiles outside the bounding box
                        v128 bboxTestMin = X86.Sse2.and_si128(X86.Sse2.cmpgt_epi32(pixelX, stxmin), X86.Sse2.cmpgt_epi32(pixelY, stymin));
                        v128 bboxTestMax = X86.Sse2.and_si128(X86.Sse2.cmpgt_epi32(stxmax, pixelX), X86.Sse2.cmpgt_epi32(stymax, pixelY));
                        v128 boxMask = X86.Sse2.and_si128(bboxTestMin, bboxTestMax);
                        zPass = X86.Sse2.and_si128(zPass, boxMask);

                        // If not all tiles failed the conservative z test we can immediately terminate the test
                        if (X86.Sse4_1.testz_si128(zPass, zPass) == 0)
                        {
                            return CullingResult.VISIBLE;
                        }

                        pixelX = X86.Sse2.add_epi32(pixelX, X86.Sse2.set1_epi32(BufferGroup.TileWidth));
                    }

                    pixelY = X86.Sse2.add_epi32(pixelY, X86.Sse2.set1_epi32(BufferGroup.TileHeight));
                }

                return CullingResult.OCCLUDED;
            }
            else
            {
                return CullingResult.VISIBLE;
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static CullingResult TestRectNEON(
            float2 min,
            float2 max,
            float wmin,
            Tile* tiles,
            BatchCullingProjectionType projectionType,
            int NumTilesX,
            v128 ScreenSize,
            v128 HalfSize,
            v128 PixelCenter
        )
        {
            if (Arm.Neon.IsNeonSupported)
            {
                v128 zero = new v128(0);
                v128 oneF = new v128(1.0f);
                v128 negOneF = new v128(-1.0f);
                v128 fullMask = new v128(~0);
                v128 wideTileWidth = new v128(BufferGroup.TileWidth);
                v128 wideTileHeight = new v128(BufferGroup.TileHeight);

                // Compute screen space bounding box and guard for out of bounds
                v128 pixelBBox = Arm.Neon.vmlaq_f32(PixelCenter, new v128(min.x, max.x, max.y, min.y), HalfSize);
                v128 pixelBBoxi = Arm.Neon.vcvtnq_s32_f32(pixelBBox);
                pixelBBoxi = Arm.Neon.vmaxq_s32(zero, Arm.Neon.vminq_s32(ScreenSize, pixelBBoxi));

                // Pad bounding box to (32xN) tiles. Tile BB is used for looping / traversal
                v128 SimdTilePad = new v128(0, BufferGroup.TileWidth, 0, BufferGroup.TileHeight);
                v128 SimdTilePadMask = new v128(
                    ~(BufferGroup.TileWidth - 1),
                    ~(BufferGroup.TileWidth - 1),
                    ~(BufferGroup.TileHeight - 1),
                    ~(BufferGroup.TileHeight - 1)
                );
                v128 tileBBoxi = Arm.Neon.vandq_s8(Arm.Neon.vaddq_s32(pixelBBoxi, SimdTilePad), SimdTilePadMask);

                int txMin = tileBBoxi.SInt0 >> BufferGroup.TileWidthShift;
                int txMax = tileBBoxi.SInt1 >> BufferGroup.TileWidthShift;
                int tileRowIdx = (tileBBoxi.SInt2 >> BufferGroup.TileHeightShift) * NumTilesX;
                int tileRowIdxEnd = (tileBBoxi.SInt3 >> BufferGroup.TileHeightShift) * NumTilesX;

                // Pad bounding box to (8x4) subtiles. Skip SIMD lanes outside the subtile BB
                v128 SimdSubTilePad = new v128(0, BufferGroup.SubTileWidth, 0, BufferGroup.SubTileHeight);
                v128 SimdSubTilePadMask = new v128(
                    ~(BufferGroup.SubTileWidth - 1),
                    ~(BufferGroup.SubTileWidth - 1),
                    ~(BufferGroup.SubTileHeight - 1),
                    ~(BufferGroup.SubTileHeight - 1)
                );
                v128 subTileBBoxi = Arm.Neon.vandq_s8(Arm.Neon.vaddq_s32(pixelBBoxi, SimdSubTilePad), SimdSubTilePadMask);

                v128 stxmin = new v128(subTileBBoxi.SInt0 - 1); // - 1 to be able to use GT test
                v128 stymin = new v128(subTileBBoxi.SInt2 - 1); // - 1 to be able to use GT test
                v128 stxmax = new v128(subTileBBoxi.SInt1);
                v128 stymax = new v128(subTileBBoxi.SInt3);

                // Setup pixel coordinates used to discard lanes outside subtile BB
                v128 SimdSubTileColOffset = new v128(
                    0,
                    BufferGroup.SubTileWidth,
                    BufferGroup.SubTileWidth * 2,
                    BufferGroup.SubTileWidth * 3
                );
                v128 startPixelX = Arm.Neon.vaddq_s32(SimdSubTileColOffset, new v128(tileBBoxi.SInt0));
                // TODO: (Apoorva) LHS is zero. We can just use the RHS directly.
                v128 pixelY = Arm.Neon.vaddq_s32(zero, new v128(tileBBoxi.SInt2));

                // Compute z from w. Note that z is reversed order, 0 = far, 1/near = near, which
                // means we use a greater than test, so zMax is used to test for visibility. (z goes from 0 = far to 2 = near for ortho)

                v128 zMax;
                v128 wMin = new v128(wmin);
                if (projectionType == BatchCullingProjectionType.Orthographic)
                {
                    zMax = Arm.Neon.vmlaq_f32(oneF, negOneF, wMin);
                }
                else
                {
                    zMax = Arm.Neon.vdivq_f32(oneF, wMin);
                }

                for (; tileRowIdx < tileRowIdxEnd; tileRowIdx += NumTilesX)
                {
                    v128 pixelX = startPixelX;

                    for (int tx = txMin; tx < txMax; tx++)
                    {
                        int tileIdx = tileRowIdx + tx;

                        // Fetch zMin from masked hierarchical Z buffer
                        v128 mask = tiles[tileIdx].mask;
                        v128 zMin0 = IntrinsicUtils._vblendq_f32(Arm.Neon.vceqq_s32(mask, fullMask), tiles[tileIdx].zMin0, tiles[tileIdx].zMin1);
                        v128 zMin1 = IntrinsicUtils._vblendq_f32(Arm.Neon.vceqq_s32(mask, zero), tiles[tileIdx].zMin1, tiles[tileIdx].zMin0);
                        v128 zBuf = Arm.Neon.vminq_f32(zMin0, zMin1);

                        // Perform conservative greater than test against hierarchical Z buffer (zMax >= zBuf means the subtile is visible)
                        v128 zPass = Arm.Neon.vcgeq_f32(zMax, zBuf);  //zPass = zMax >= zBuf ? ~0 : 0

                        // Mask out lanes corresponding to subtiles outside the bounding box
                        v128 bboxTestMin = Arm.Neon.vandq_s8(Arm.Neon.vcgtq_s32(pixelX, stxmin), Arm.Neon.vcgtq_s32(pixelY, stymin));
                        v128 bboxTestMax = Arm.Neon.vandq_s8(Arm.Neon.vcgtq_s32(stxmax, pixelX), Arm.Neon.vcgtq_s32(stymax, pixelY));
                        v128 boxMask = Arm.Neon.vandq_s8(bboxTestMin, bboxTestMax);
                        zPass = Arm.Neon.vandq_s8(zPass, boxMask);

                        // If not all tiles failed the conservative z test we can immediately terminate the test
                        v64 zTestResult = Arm.Neon.vqmovn_u64(zPass);
                        if (zTestResult.ULong0 != 0ul)
                        {
                            return CullingResult.VISIBLE;
                        }

                        pixelX = Arm.Neon.vaddq_s32(pixelX, wideTileWidth);
                    }

                    pixelY = Arm.Neon.vaddq_s32(pixelY, wideTileHeight);
                }

                return CullingResult.OCCLUDED;
            }
            else
            {
                return CullingResult.VISIBLE;
            }
        }

        public static CullingResult TestRect(
            float2 min,
            float2 max,
            float wmin,
            Tile* tiles,
            BatchCullingProjectionType projectionType,
            int NumTilesX,
            v128 ScreenSize,
            v128 HalfSize,
            v128 PixelCenter
        )
        {
            if (min.x > 1.0f || min.y > 1.0f || max.x < -1.0f || max.y < -1.0f)
            {
                return CullingResult.VIEW_CULLED;
            }

            if (X86.Sse4_1.IsSse41Supported)
            {
                return TestRectSSE( min, max, wmin, tiles, projectionType, NumTilesX, ScreenSize, HalfSize, PixelCenter );
            }
            else if (Arm.Neon.IsNeonSupported)
            {
                return TestRectNEON( min, max, wmin, tiles, projectionType, NumTilesX, ScreenSize, HalfSize, PixelCenter );
            }

            return CullingResult.VISIBLE;
        }
    }
}

#endif // ENABLE_UNITY_OCCLUSION && (HDRP_10_0_0_OR_NEWER || URP_10_0_0_OR_NEWER)
