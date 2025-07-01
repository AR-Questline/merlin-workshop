#if ENABLE_UNITY_OCCLUSION && (HDRP_10_0_0_OR_NEWER || URP_10_0_0_OR_NEWER)

using System.Runtime.CompilerServices;
using Unity.Burst;
using Unity.Burst.Intrinsics;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;
using Unity.Rendering.Occlusion.Masked.Dots;
using UnityEngine.Rendering;

namespace Unity.Rendering.Occlusion.Masked
{
    [BurstCompile(DisableSafetyChecks = true)]
    unsafe struct RasterizeJob : IJobFor
    {
        [ReadOnly] public NativeArray<ClippedOccluder> ClippedOccluders;
        [ReadOnly] public NativeArray<float3> ClippedVerts;
        [ReadOnly] public NativeArray<float4> ClippedTriExtents;
        [ReadOnly] public BatchCullingProjectionType ProjectionType;
        [ReadOnly] public int NumBuffers;
        [ReadOnly] public v128 HalfWidth;
        [ReadOnly] public v128 HalfHeight;
        [ReadOnly] public v128 PixelCenterX;
        [ReadOnly] public v128 PixelCenterY;
        [ReadOnly] public v128 PixelCenter;
        [ReadOnly] public v128 HalfSize;
        [ReadOnly] public v128 ScreenSize;
        [ReadOnly] public int BinSize;
        [ReadOnly] public int NumPixelsX;
        [ReadOnly] public int NumPixelsY;
        [ReadOnly] public int NumTilesX;
        [ReadOnly] public int NumTilesY;
        [ReadOnly] public float NearClip;
        [ReadOnly, NativeDisableUnsafePtrRestriction] public v128* FrustumPlanes;
        [ReadOnly] public ScissorRect FullScreenScissor;

        [NativeSetThreadIndex] private int WorkerIndex;

        // A bin is a screen area formed by X * Y tiles, a tile is the minimum pixels that we
        // process in the system
        [ReadOnly] public int TilesPerBinX;
        [ReadOnly] public int TilesPerBinY;
        // A buffer group contains a bunch of contiguous tile-buffers. This pointer points to the base of the one we're
        // rendering to.
        [NativeDisableUnsafePtrRestriction] public Tile*    TilesBasePtr;
        [NativeDisableUnsafePtrRestriction] public float*   BinTriangleXBasePtr;
        [NativeDisableUnsafePtrRestriction] public float*   BinTriangleYBasePtr;
        [NativeDisableUnsafePtrRestriction] public float*   BinTriangleWBasePtr;

        const int MAX_CLIPPED = 32;
        const int SIMD_LANES = 4;
        const int SIMD_ALL_LANES_MASK = (1 << SIMD_LANES) - 1;
        const int BIG_TRIANGLE = 3;

        #region SSE
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void TraverseScanlineSSE(Tile* tiles, int numRight, int numLeft, int leftOffset, int rightOffset, int tileIdx, int rightEvent, int leftEvent, v128* events, v128 zTriMin, v128 zTriMax, v128 iz0, float zx)
        {
            if (X86.Sse4_1.IsSse41Supported)
            {
                v128* right = stackalloc v128[numRight];
                v128* left = stackalloc v128[numLeft];

                // Floor edge events to integer pixel coordinates (shift out fixed point bits)
                v128 eventOffset = new v128(leftOffset << BufferGroup.TileWidthShift);
                v128 emptyBitMask = new v128(0);
                v128 fullBitMask = new v128(~0);
                v128 simdTileWidth = X86.Sse2.set1_epi32(BufferGroup.TileWidth);

                for (int i = 0; i < numRight; ++i)
                {
                    right[i] = X86.Sse4_1.max_epi32(X86.Sse2.sub_epi32(X86.Sse2.srai_epi32(events[rightEvent + i], BufferGroup.FpBits), eventOffset), emptyBitMask);
                }

                for (int i = 0; i < numLeft; ++i)
                {
                    left[i] = X86.Sse4_1.max_epi32(X86.Sse2.sub_epi32(X86.Sse2.srai_epi32(events[leftEvent - i], BufferGroup.FpBits), eventOffset), emptyBitMask);
                }

                v128 z0 = X86.Sse.add_ps(iz0, X86.Sse.set1_ps(zx * leftOffset));
                int tileIdxEnd = tileIdx + rightOffset;
                tileIdx += leftOffset;

                for (; ; )
                {
                    // Compute zMin for the overlapped layers
                    v128 mask = tiles[tileIdx].mask;
                    v128 zMin0 = X86.Sse4_1.blendv_ps(tiles[tileIdx].zMin0, tiles[tileIdx].zMin1, X86.Sse2.cmpeq_epi32(mask, fullBitMask));
                    v128 zMin1 = X86.Sse4_1.blendv_ps(tiles[tileIdx].zMin1, tiles[tileIdx].zMin0, X86.Sse2.cmpeq_epi32(mask, emptyBitMask));
                    v128 zMinBuf = X86.Sse.min_ps(zMin0, zMin1);
                    v128 dist0 = X86.Sse.sub_ps(zTriMax, zMinBuf);

                    if (X86.Sse.movemask_ps(dist0) != SIMD_ALL_LANES_MASK)
                    {
                        // Compute coverage mask for entire 32xN using shift operations
                        v128 accumulatedMask = IntrinsicUtils._mmw_sllv_ones(left[0]);

                        for (int i = 1; i < numLeft; ++i)
                        {
                            accumulatedMask = X86.Sse2.and_si128(accumulatedMask, IntrinsicUtils._mmw_sllv_ones(left[i]));
                        }

                        for (int i = 0; i < numRight; ++i)
                        {
                            accumulatedMask = X86.Sse2.andnot_si128(IntrinsicUtils._mmw_sllv_ones(right[i]), accumulatedMask);
                        }

                        // Compute interpolated min for each 8x4 subtile and update the masked hierarchical z buffer entry
                        v128 zSubTileMin = X86.Sse.max_ps(z0, zTriMin);
                        UpdateTileAccurateSSE(tiles, tileIdx, IntrinsicUtils._mmw_transpose_epi8(accumulatedMask), zSubTileMin);
                    }

                    // Update buffer address, interpolate z and edge events
                    tileIdx++;

                    if (tileIdx >= tileIdxEnd)
                    {
                        break;
                    }

                    z0 = X86.Sse.add_ps(z0, X86.Sse.set1_ps(zx));

                    for (int i = 0; i < numRight; ++i)
                    {
                        right[i] = X86.Sse2.subs_epu16(right[i], simdTileWidth);  // Trick, use sub saturated to avoid checking against < 0 for shift (values should fit in 16 bits)
                    }

                    for (int i = 0; i < numLeft; ++i)
                    {
                        left[i] = X86.Sse2.subs_epu16(left[i], simdTileWidth);
                    }
                }
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        void UpdateTileAccurateSSE(Tile* tiles, int tileIdx, v128 coverage, v128 zTriv)
        {
            if (X86.Sse4_1.IsSse41Supported)
            {
                v128 zMin0 = tiles[tileIdx].zMin0;
                v128 zMin1 = tiles[tileIdx].zMin1;
                v128 mask = tiles[tileIdx].mask;

                // Swizzle coverage mask to 8x4 subtiles
                v128 rastMask = coverage;

                // Perform individual depth tests with layer 0 & 1 and mask out all failing pixels
                v128 sdist0 = X86.Sse.sub_ps(zMin0, zTriv);
                v128 sdist1 = X86.Sse.sub_ps(zMin1, zTriv);
                v128 sign0 = X86.Sse2.srai_epi32(sdist0, 31);
                v128 sign1 = X86.Sse2.srai_epi32(sdist1, 31);
                v128 triMask = X86.Sse2.and_si128(rastMask, X86.Sse2.or_si128(X86.Sse2.andnot_si128(mask, sign0), X86.Sse2.and_si128(mask, sign1)));

                // Early out if no pixels survived the depth test (this test is more accurate than
                // the early culling test in TraverseScanline())
                v128 t0 = X86.Sse2.cmpeq_epi32(triMask, X86.Sse2.setzero_si128());
                v128 t0inv = /*not_epi32*/ X86.Sse2.xor_si128(t0, X86.Sse2.set1_epi32(~0));

                if (X86.Sse4_1.testz_si128(t0inv, t0inv) != 0)
                {
                    return;
                }

#if MOC_ENABLE_STATS
                STATS_ADD(ref mStats.mOccluders.mNumTilesUpdated, 1);
#endif

                v128 zTri = X86.Sse4_1.blendv_ps(zTriv, zMin0, t0);

                // Test if incoming triangle completely overwrites layer 0 or 1
                v128 layerMask0 = X86.Sse2.andnot_si128(triMask, /*not_epi32*/ X86.Sse2.xor_si128(mask, X86.Sse2.set1_epi32(~0)));
                v128 layerMask1 = X86.Sse2.andnot_si128(triMask, mask);
                v128 lm0 = X86.Sse2.cmpeq_epi32(layerMask0, X86.Sse2.setzero_si128());
                v128 lm1 = X86.Sse2.cmpeq_epi32(layerMask1, X86.Sse2.setzero_si128());
                v128 z0 = X86.Sse4_1.blendv_ps(zMin0, zTri, lm0);
                v128 z1 = X86.Sse4_1.blendv_ps(zMin1, zTri, lm1);

                // Compute distances used for merging heuristic
                v128 d0 = /*abs_ps*/ X86.Sse.and_ps(sdist0, X86.Sse2.set1_epi32(0x7FFFFFFF));
                v128 d1 = /*abs_ps*/ X86.Sse.and_ps(sdist1, X86.Sse2.set1_epi32(0x7FFFFFFF));
                v128 d2 = /*abs_ps*/ X86.Sse.and_ps(X86.Sse.sub_ps(z0, z1), X86.Sse2.set1_epi32(0x7FFFFFFF));

                // Find minimum distance
                v128 c01 = X86.Sse.sub_ps(d0, d1);
                v128 c02 = X86.Sse.sub_ps(d0, d2);
                v128 c12 = X86.Sse.sub_ps(d1, d2);
                // Two tests indicating which layer the incoming triangle will merge with or
                // overwrite. d0min indicates that the triangle will overwrite layer 0, and
                // d1min flags that the triangle will overwrite layer 1.
                v128 d0min = X86.Sse2.or_si128(X86.Sse2.and_si128(c01, c02), X86.Sse2.or_si128(lm0, t0));
                v128 d1min = X86.Sse2.andnot_si128(d0min, X86.Sse2.or_si128(c12, lm1));

                /* Update depth buffer entry. NOTE: we always merge into layer 0, so if the
                   triangle should be merged with layer 1, we first swap layer 0 & 1 and then
                   merge into layer 0. */

                // Update mask based on which layer the triangle overwrites or was merged into
                v128 inner = X86.Sse4_1.blendv_ps(triMask, layerMask1, d0min);

                // Update the zMin[0] value. There are four outcomes: overwrite with layer 1,
                // merge with layer 1, merge with zTri or overwrite with layer 1 and then merge
                // with zTri.
                v128 e0 = X86.Sse4_1.blendv_ps(z0, z1, d1min);
                v128 e1 = X86.Sse4_1.blendv_ps(z1, zTri, X86.Sse2.or_si128(d1min, d0min));

                // Update the zMin[1] value. There are three outcomes: keep current value,
                // overwrite with zTri, or overwrite with z1
                v128 z1t = X86.Sse4_1.blendv_ps(zTri, z1, d0min);

                tiles[tileIdx].zMin0 = X86.Sse.min_ps(e0, e1);
                tiles[tileIdx].zMin1 = X86.Sse4_1.blendv_ps(z1t, z0, d1min);
                tiles[tileIdx].mask = X86.Sse4_1.blendv_ps(inner, layerMask0, d1min);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void UpdateTileEventsYSSE(v128* triEventRemainder, v128* triSlopeTileRemainder, v128* triEdgeY, v128* triEvent, v128* triSlopeTileDelta, v128* triSlopeSign, int i)
        {
            if (X86.Sse2.IsSse2Supported)
            {
                triEventRemainder[i] = X86.Sse2.sub_epi32(triEventRemainder[i], triSlopeTileRemainder[i]);
                v128 overflow = X86.Sse2.srai_epi32(triEventRemainder[i], 31);
                triEventRemainder[i] = X86.Sse2.add_epi32(triEventRemainder[i], X86.Sse2.and_si128(overflow, triEdgeY[i]));
                triEvent[i] = X86.Sse2.add_epi32(triEvent[i], X86.Sse2.add_epi32(triSlopeTileDelta[i], X86.Sse2.and_si128(overflow, triSlopeSign[i])));
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void SortVerticesSSE(v128* vX, v128* vY)
        {
            if (X86.Sse4_1.IsSse41Supported)
            {
                // Rotate the triangle in the winding order until v0 is the vertex with lowest Y value
                for (int i = 0; i < 2; i++)
                {
                    v128 ey1 = X86.Sse2.sub_epi32(vY[1], vY[0]);
                    v128 ey2 = X86.Sse2.sub_epi32(vY[2], vY[0]);
                    v128 swapMask = X86.Sse2.or_si128(X86.Sse2.or_si128(ey1, ey2), X86.Sse2.cmpeq_epi32(ey2, X86.Sse2.setzero_si128()));

                    v128 sX = X86.Sse4_1.blendv_ps(vX[2], vX[0], swapMask);
                    vX[0] = X86.Sse4_1.blendv_ps(vX[0], vX[1], swapMask);
                    vX[1] = X86.Sse4_1.blendv_ps(vX[1], vX[2], swapMask);
                    vX[2] = sX;

                    v128 sY = X86.Sse4_1.blendv_ps(vY[2], vY[0], swapMask);
                    vY[0] = X86.Sse4_1.blendv_ps(vY[0], vY[1], swapMask);
                    vY[1] = X86.Sse4_1.blendv_ps(vY[1], vY[2], swapMask);
                    vY[2] = sY;
                }
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void ComputeDepthPlaneSSE(v128* pVtxX, v128* pVtxY, v128* pVtxZ, out v128 zPixelDx, out v128 zPixelDy)
        {
            if (X86.Sse.IsSseSupported)
            {
                // Setup z(x,y) = z0 + dx*x + dy*y screen space depth plane equation
                v128 x2 = X86.Sse.sub_ps(pVtxX[2], pVtxX[0]);
                v128 x1 = X86.Sse.sub_ps(pVtxX[1], pVtxX[0]);
                v128 y1 = X86.Sse.sub_ps(pVtxY[1], pVtxY[0]);
                v128 y2 = X86.Sse.sub_ps(pVtxY[2], pVtxY[0]);
                v128 z1 = X86.Sse.sub_ps(pVtxZ[1], pVtxZ[0]);
                v128 z2 = X86.Sse.sub_ps(pVtxZ[2], pVtxZ[0]);
                v128 d = X86.Sse.div_ps(X86.Sse.set1_ps(1.0f), IntrinsicUtils._mmw_fmsub_ps(x1, y2, X86.Sse.mul_ps(y1, x2)));
                zPixelDx = X86.Sse.mul_ps(IntrinsicUtils._mmw_fmsub_ps(z1, y2, X86.Sse.mul_ps(y1, z2)), d);
                zPixelDy = X86.Sse.mul_ps(IntrinsicUtils._mmw_fmsub_ps(x1, z2, X86.Sse.mul_ps(z1, x2)), d);
            }
            else
            {
                zPixelDx = new v128();
                zPixelDy = new v128();
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void ComputeBoundingBoxSSE(v128* vX, v128* vY, ref ScissorRect scissor, out v128 bbminX, out v128 bbminY, out v128 bbmaxX, out v128 bbmaxY)
        {
            if (X86.Sse4_1.IsSse41Supported)
            {
                // Find Min/Max vertices
                bbminX = X86.Sse2.cvttps_epi32(X86.Sse.min_ps(vX[0], X86.Sse.min_ps(vX[1], vX[2])));
                bbminY = X86.Sse2.cvttps_epi32(X86.Sse.min_ps(vY[0], X86.Sse.min_ps(vY[1], vY[2])));
                bbmaxX = X86.Sse2.cvttps_epi32(X86.Sse.max_ps(vX[0], X86.Sse.max_ps(vX[1], vX[2])));
                bbmaxY = X86.Sse2.cvttps_epi32(X86.Sse.max_ps(vY[0], X86.Sse.max_ps(vY[1], vY[2])));

                // Clamp to tile boundaries
                v128 SimdPadWMask = X86.Sse2.set1_epi32(~(BufferGroup.TileWidth - 1));
                v128 SimdPadHMask = X86.Sse2.set1_epi32(~(BufferGroup.TileHeight - 1));
                bbminX = X86.Sse2.and_si128(bbminX, SimdPadWMask);
                bbmaxX = X86.Sse2.and_si128(X86.Sse2.add_epi32(bbmaxX, X86.Sse2.set1_epi32(BufferGroup.TileWidth)), SimdPadWMask);
                bbminY = X86.Sse2.and_si128(bbminY, SimdPadHMask);
                bbmaxY = X86.Sse2.and_si128(X86.Sse2.add_epi32(bbmaxY, X86.Sse2.set1_epi32(BufferGroup.TileHeight)), SimdPadHMask);

                // Clip to scissor
                bbminX = X86.Sse4_1.max_epi32(bbminX, X86.Sse2.set1_epi32(scissor.mMinX));
                bbmaxX = X86.Sse4_1.min_epi32(bbmaxX, X86.Sse2.set1_epi32(scissor.mMaxX));
                bbminY = X86.Sse4_1.max_epi32(bbminY, X86.Sse2.set1_epi32(scissor.mMinY));
                bbmaxY = X86.Sse4_1.min_epi32(bbmaxY, X86.Sse2.set1_epi32(scissor.mMaxY));
            }
            else
            {
                bbminX = new v128();
                bbminY = new v128();
                bbmaxX = new v128();
                bbmaxY = new v128();
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        void ProjectVerticesSSE(v128* ipVtxX, v128* ipVtxY, v128* pVtxX, v128* pVtxY, v128* pVtxZ, v128* vtxX, v128* vtxY, v128* vtxW)
        {
            if (X86.Sse2.IsSse2Supported)
            {
                const float FP_INV = 1f / (1 << BufferGroup.FpBits);
                // Project vertices and transform to screen space. Snap to sub-pixel coordinates with BufferGroup.FpBits precision.
                for (int i = 0; i < 3; i++)
                {
                    int idx = 2 - i;
                    v128 rcpW;

                    if (ProjectionType == BatchCullingProjectionType.Orthographic)
                    {
                        rcpW = IntrinsicUtils._mmw_fmadd_ps(X86.Sse.set1_ps(-1.0f), vtxW[i], X86.Sse.set1_ps(1.0f));

                        v128 screenX = IntrinsicUtils._mmw_fmadd_ps(vtxX[i], HalfWidth, PixelCenterX);
                        v128 screenY = IntrinsicUtils._mmw_fmadd_ps(vtxY[i], HalfHeight, PixelCenterY);
                        ipVtxX[idx] = X86.Sse2.cvtps_epi32(X86.Sse.mul_ps(screenX, X86.Sse.set1_ps((float)(1 << BufferGroup.FpBits))));
                        ipVtxY[idx] = X86.Sse2.cvtps_epi32(X86.Sse.mul_ps(screenY, X86.Sse.set1_ps((float)(1 << BufferGroup.FpBits))));
                    }
                    else
                    {
                        rcpW = X86.Sse.div_ps(X86.Sse.set1_ps(1f), vtxW[i]);

                        v128 screenX = IntrinsicUtils._mmw_fmadd_ps(X86.Sse.mul_ps(vtxX[i], HalfWidth), rcpW, PixelCenterX);
                        v128 screenY = IntrinsicUtils._mmw_fmadd_ps(X86.Sse.mul_ps(vtxY[i], HalfHeight), rcpW, PixelCenterY);

                        ipVtxX[idx] = X86.Sse2.cvtps_epi32(X86.Sse.mul_ps(screenX, X86.Sse.set1_ps((float)(1 << BufferGroup.FpBits))));
                        ipVtxY[idx] = X86.Sse2.cvtps_epi32(X86.Sse.mul_ps(screenY, X86.Sse.set1_ps((float)(1 << BufferGroup.FpBits))));
                    }

                    pVtxX[idx] = X86.Sse.mul_ps(X86.Sse2.cvtepi32_ps(ipVtxX[idx]), X86.Sse.set1_ps(FP_INV));
                    pVtxY[idx] = X86.Sse.mul_ps(X86.Sse2.cvtepi32_ps(ipVtxY[idx]), X86.Sse.set1_ps(FP_INV));
                    pVtxZ[idx] = rcpW;
                }
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void RasterizeTriangleBatchSSE(Tile* tiles, v128* ipVtxX, v128* ipVtxY, v128* pVtxX, v128* pVtxY, v128* pVtxZ, uint triMask, ScissorRect scissor)
        {
            if (X86.Sse4_1.IsSse41Supported)
            {
                //we are computing the bounding box again when we used it before but there are some use cases after, this check cannot be removed atm

                // Compute bounding box and clamp to tile coordinates
                ComputeBoundingBoxSSE(pVtxX, pVtxY, ref scissor, out var bbPixelMinX, out var bbPixelMinY, out var bbPixelMaxX, out var bbPixelMaxY);

                // Clamp bounding box to tiles (it's already padded in computeBoundingBox)
                v128 bbTileMinX = X86.Sse2.srai_epi32(bbPixelMinX, BufferGroup.TileWidthShift);
                v128 bbTileMinY = X86.Sse2.srai_epi32(bbPixelMinY, BufferGroup.TileHeightShift);
                v128 bbTileMaxX = X86.Sse2.srai_epi32(bbPixelMaxX, BufferGroup.TileWidthShift);
                v128 bbTileMaxY = X86.Sse2.srai_epi32(bbPixelMaxY, BufferGroup.TileHeightShift);
                v128 bbTileSizeX = X86.Sse2.sub_epi32(bbTileMaxX, bbTileMinX);
                v128 bbTileSizeY = X86.Sse2.sub_epi32(bbTileMaxY, bbTileMinY);

                // Cull triangles with zero bounding box
                v128 bboxSign = X86.Sse2.or_si128(X86.Sse2.sub_epi32(bbTileSizeX, X86.Sse2.set1_epi32(1)), X86.Sse2.sub_epi32(bbTileSizeY, X86.Sse2.set1_epi32(1)));
                triMask &= (uint)((~X86.Sse.movemask_ps(bboxSign)) & SIMD_ALL_LANES_MASK);

                if (triMask == 0x0)
                {
                    return; // View-culled
                }

                // Set up screen space depth plane
                ComputeDepthPlaneSSE(pVtxX, pVtxY, pVtxZ, out var zPixelDx, out var zPixelDy);

                // Compute z value at min corner of bounding box. Offset to make sure z is conservative for all 8x4 subtiles
                v128 bbMinXV0 = X86.Sse.sub_ps(X86.Sse2.cvtepi32_ps(bbPixelMinX), pVtxX[0]);
                v128 bbMinYV0 = X86.Sse.sub_ps(X86.Sse2.cvtepi32_ps(bbPixelMinY), pVtxY[0]);
                v128 zPlaneOffset = IntrinsicUtils._mmw_fmadd_ps(zPixelDx, bbMinXV0, IntrinsicUtils._mmw_fmadd_ps(zPixelDy, bbMinYV0, pVtxZ[0]));
                v128 zTileDx = X86.Sse.mul_ps(zPixelDx, X86.Sse.set1_ps(BufferGroup.TileWidth));
                v128 zTileDy = X86.Sse.mul_ps(zPixelDy, X86.Sse.set1_ps(BufferGroup.TileHeight));

                zPlaneOffset = X86.Sse.add_ps(zPlaneOffset, X86.Sse.min_ps(X86.Sse2.setzero_si128(), X86.Sse.mul_ps(zPixelDx, X86.Sse.set1_ps(BufferGroup.SubTileWidth))));
                zPlaneOffset = X86.Sse.add_ps(zPlaneOffset, X86.Sse.min_ps(X86.Sse2.setzero_si128(), X86.Sse.mul_ps(zPixelDy, X86.Sse.set1_ps(BufferGroup.SubTileHeight))));

                // Compute Zmin and Zmax for the triangle (used to narrow the range for difficult tiles)
                v128 zMin = X86.Sse.min_ps(pVtxZ[0], X86.Sse.min_ps(pVtxZ[1], pVtxZ[2]));
                v128 zMax = X86.Sse.max_ps(pVtxZ[0], X86.Sse.max_ps(pVtxZ[1], pVtxZ[2]));

                /* Sort vertices (v0 has lowest Y, and the rest is in winding order) and compute edges. Also find the middle
                    vertex and compute tile */

                // Rotate the triangle in the winding order until v0 is the vertex with lowest Y value
                SortVerticesSSE(ipVtxX, ipVtxY);

                // Compute edges
                v128* edgeX = stackalloc v128[3];
                edgeX[0] = X86.Sse2.sub_epi32(ipVtxX[1], ipVtxX[0]);
                edgeX[1] = X86.Sse2.sub_epi32(ipVtxX[2], ipVtxX[1]);
                edgeX[2] = X86.Sse2.sub_epi32(ipVtxX[2], ipVtxX[0]);

                v128* edgeY = stackalloc v128[3];
                edgeY[0] = X86.Sse2.sub_epi32(ipVtxY[1], ipVtxY[0]);
                edgeY[1] = X86.Sse2.sub_epi32(ipVtxY[2], ipVtxY[1]);
                edgeY[2] = X86.Sse2.sub_epi32(ipVtxY[2], ipVtxY[0]);

                // Classify if the middle vertex is on the left or right and compute its position
                int midVtxRight = ~X86.Sse.movemask_ps(edgeY[1]);
                v128 midPixelX = X86.Sse4_1.blendv_ps(ipVtxX[1], ipVtxX[2], edgeY[1]);
                v128 midPixelY = X86.Sse4_1.blendv_ps(ipVtxY[1], ipVtxY[2], edgeY[1]);
                v128 midTileY = X86.Sse2.srai_epi32(X86.Sse4_1.max_epi32(midPixelY, X86.Sse2.setzero_si128()), BufferGroup.TileHeightShift + BufferGroup.FpBits);
                v128 bbMidTileY = X86.Sse4_1.max_epi32(bbTileMinY, X86.Sse4_1.min_epi32(bbTileMaxY, midTileY));

                // Compute edge events for the bottom of the bounding box, or for the middle tile in case of
                // the edge originating from the middle vertex.
                v128* xDiffi = stackalloc v128[2];
                xDiffi[0] = X86.Sse2.sub_epi32(ipVtxX[0], X86.Sse2.slli_epi32(bbPixelMinX, BufferGroup.FpBits));
                xDiffi[1] = X86.Sse2.sub_epi32(midPixelX, X86.Sse2.slli_epi32(bbPixelMinX, BufferGroup.FpBits));

                v128* yDiffi = stackalloc v128[2];
                yDiffi[0] = X86.Sse2.sub_epi32(ipVtxY[0], X86.Sse2.slli_epi32(bbPixelMinY, BufferGroup.FpBits));
                yDiffi[1] = X86.Sse2.sub_epi32(midPixelY, X86.Sse2.slli_epi32(bbMidTileY, BufferGroup.FpBits + BufferGroup.TileHeightShift));

                /* Edge slope setup - Note we do not conform to DX/GL rasterization rules */

                // Potentially flip edge to ensure that all edges have positive Y slope.
                edgeX[1] = X86.Sse4_1.blendv_ps(edgeX[1], /*neg_epi32*/ X86.Sse2.sub_epi32(X86.Sse2.set1_epi32(0), edgeX[1]), edgeY[1]);
                edgeY[1] = X86.Ssse3.abs_epi32(edgeY[1]);

                // Compute floating point slopes
                v128* slope = stackalloc v128[3];
                slope[0] = X86.Sse.div_ps(X86.Sse2.cvtepi32_ps(edgeX[0]), X86.Sse2.cvtepi32_ps(edgeY[0]));
                slope[1] = X86.Sse.div_ps(X86.Sse2.cvtepi32_ps(edgeX[1]), X86.Sse2.cvtepi32_ps(edgeY[1]));
                slope[2] = X86.Sse.div_ps(X86.Sse2.cvtepi32_ps(edgeX[2]), X86.Sse2.cvtepi32_ps(edgeY[2]));

                // Modify slope of horizontal edges to make sure they mask out pixels above/below the edge. The slope is set to screen
                // width to mask out all pixels above or below the horizontal edge. We must also add a small bias to acount for that
                // vertices may end up off screen due to clipping. We're assuming that the round off error is no bigger than 1.0
                v128 horizontalSlopeDelta = X86.Sse.set1_ps(2f * (NumPixelsX + 2f * (BufferGroup.GuardBandPixelSize + 1.0f)));
                v128 horizontalSlope0 = X86.Sse2.cmpeq_epi32(edgeY[0], X86.Sse2.setzero_si128());
                v128 horizontalSlope1 = X86.Sse2.cmpeq_epi32(edgeY[1], X86.Sse2.setzero_si128());
                slope[0] = X86.Sse4_1.blendv_ps(slope[0], horizontalSlopeDelta, horizontalSlope0);
                slope[1] = X86.Sse4_1.blendv_ps(slope[1], /*neg_ps*/ X86.Sse.xor_ps(horizontalSlopeDelta, X86.Sse.set1_ps(-0f)), horizontalSlope1);

                v128* vy = stackalloc v128[3];
                vy[0] = yDiffi[0];
                vy[1] = yDiffi[1];
                vy[2] = yDiffi[0];

                v128 offset0 = X86.Sse2.and_si128(X86.Sse2.add_epi32(yDiffi[0], X86.Sse2.set1_epi32(BufferGroup.FpHalfPixel - 1)), X86.Sse2.set1_epi32((-1 << BufferGroup.FpBits)));
                v128 offset1 = X86.Sse2.and_si128(X86.Sse2.add_epi32(yDiffi[1], X86.Sse2.set1_epi32(BufferGroup.FpHalfPixel - 1)), X86.Sse2.set1_epi32((-1 << BufferGroup.FpBits)));
                vy[0] = X86.Sse4_1.blendv_ps(yDiffi[0], offset0, horizontalSlope0);
                vy[1] = X86.Sse4_1.blendv_ps(yDiffi[1], offset1, horizontalSlope1);

                // Compute edge events for the bottom of the bounding box, or for the middle tile in case of
                // the edge originating from the middle vertex.
                v128* slopeSign = stackalloc v128[3];
                v128* absEdgeX = stackalloc v128[3];
                v128* slopeTileDelta = stackalloc v128[3];
                v128* eventStartRemainder = stackalloc v128[3];
                v128* slopeTileRemainder = stackalloc v128[3];
                v128* eventStart = stackalloc v128[3];

                for (int i = 0; i < 3; i++)
                {
                    // Common, compute slope sign (used to propagate the remainder term when overflowing) is postive or negative x-direction
                    slopeSign[i] = X86.Sse4_1.blendv_ps(X86.Sse2.set1_epi32(1), X86.Sse2.set1_epi32(-1), edgeX[i]);
                    absEdgeX[i] = X86.Ssse3.abs_epi32(edgeX[i]);

                    // Delta and error term for one vertical tile step. The exact delta is exactDelta = edgeX / edgeY, due to limited precision we
                    // repersent the delta as delta = qoutient + remainder / edgeY, where quotient = int(edgeX / edgeY). In this case, since we step
                    // one tile of scanlines at a time, the slope is computed for a tile-sized step.
                    slopeTileDelta[i] = X86.Sse2.cvttps_epi32(X86.Sse.mul_ps(slope[i], X86.Sse.set1_ps(BufferGroup.FpTileHeight)));
                    slopeTileRemainder[i] = X86.Sse2.sub_epi32(X86.Sse2.slli_epi32(absEdgeX[i], BufferGroup.FpTileHeightShift), X86.Sse4_1.mullo_epi32(X86.Ssse3.abs_epi32(slopeTileDelta[i]), edgeY[i]));

                    // Jump to bottom scanline of tile row, this is the bottom of the bounding box, or the middle vertex of the triangle.
                    // The jump can be in both positive and negative y-direction due to clipping / offscreen vertices.
                    v128 tileStartDir = X86.Sse4_1.blendv_ps(slopeSign[i], /*neg_epi32*/ X86.Sse2.sub_epi32(X86.Sse2.set1_epi32(0), slopeSign[i]), vy[i]);
                    v128 tieBreaker = X86.Sse4_1.blendv_ps(X86.Sse2.set1_epi32(0), X86.Sse2.set1_epi32(1), tileStartDir);
                    v128 tileStartSlope = X86.Sse2.cvttps_epi32(X86.Sse.mul_ps(slope[i], X86.Sse2.cvtepi32_ps(/*neg_epi32*/ X86.Sse2.sub_epi32(X86.Sse2.set1_epi32(0), vy[i]))));
                    v128 tileStartRemainder = X86.Sse2.sub_epi32(X86.Sse4_1.mullo_epi32(absEdgeX[i], X86.Ssse3.abs_epi32(vy[i])), X86.Sse4_1.mullo_epi32(X86.Ssse3.abs_epi32(tileStartSlope), edgeY[i]));

                    eventStartRemainder[i] = X86.Sse2.sub_epi32(tileStartRemainder, tieBreaker);
                    v128 overflow = X86.Sse2.srai_epi32(eventStartRemainder[i], 31);
                    eventStartRemainder[i] = X86.Sse2.add_epi32(eventStartRemainder[i], X86.Sse2.and_si128(overflow, edgeY[i]));
                    eventStartRemainder[i] = X86.Sse4_1.blendv_ps(eventStartRemainder[i], X86.Sse2.sub_epi32(X86.Sse2.sub_epi32(edgeY[i], eventStartRemainder[i]), X86.Sse2.set1_epi32(1)), vy[i]);

                    //eventStart[i] = xDiffi[i & 1] + tileStartSlope + (overflow & tileStartDir) + X86.Sse2.set1_epi32(FP_HALF_PIXEL - 1) + tieBreaker;
                    eventStart[i] = X86.Sse2.add_epi32(X86.Sse2.add_epi32(xDiffi[i & 1], tileStartSlope), X86.Sse2.and_si128(overflow, tileStartDir));
                    eventStart[i] = X86.Sse2.add_epi32(X86.Sse2.add_epi32(eventStart[i], X86.Sse2.set1_epi32(BufferGroup.FpHalfPixel - 1)), tieBreaker);
                }

                // Split bounding box into bottom - middle - top region.
                v128 bbBottomIdx = X86.Sse2.add_epi32(bbTileMinX, X86.Sse4_1.mullo_epi32(bbTileMinY, X86.Sse2.set1_epi32(NumTilesX)));
                v128 bbTopIdx = X86.Sse2.add_epi32(bbTileMinX, X86.Sse4_1.mullo_epi32(X86.Sse2.add_epi32(bbTileMinY, bbTileSizeY), X86.Sse2.set1_epi32(NumTilesX)));
                v128 bbMidIdx = X86.Sse2.add_epi32(bbTileMinX, X86.Sse4_1.mullo_epi32(midTileY, X86.Sse2.set1_epi32(NumTilesX)));

                // Loop over non-culled triangle and change SIMD axis to per-pixel
                while (triMask != 0)
                {
                    uint triIdx = (uint)IntrinsicUtils.find_clear_lsb(ref triMask);
                    int triMidVtxRight = (midVtxRight >> (int)triIdx) & 1;

                    // Get Triangle Zmin zMax
                    v128 zTriMax = X86.Sse.set1_ps(IntrinsicUtils.getFloatLane(zMax, triIdx));
                    v128 zTriMin = X86.Sse.set1_ps(IntrinsicUtils.getFloatLane(zMin, triIdx));

                    // Setup Zmin value for first set of 8x4 subtiles
                    v128 SimdSubTileColOffsetF = X86.Sse.setr_ps(0, BufferGroup.SubTileWidth, BufferGroup.SubTileWidth * 2, BufferGroup.SubTileWidth * 3);
                    v128 z0 = IntrinsicUtils._mmw_fmadd_ps(X86.Sse.set1_ps(IntrinsicUtils.getFloatLane(zPixelDx, triIdx)),
                                                            SimdSubTileColOffsetF,
                                                            IntrinsicUtils._mmw_fmadd_ps(X86.Sse.set1_ps(IntrinsicUtils.getFloatLane(zPixelDy, triIdx)),
                                                            X86.Sse2.setzero_si128(),
                                                            X86.Sse.set1_ps(IntrinsicUtils.getFloatLane(zPlaneOffset, triIdx))));

                    float zx = IntrinsicUtils.getFloatLane(zTileDx, triIdx);
                    float zy = IntrinsicUtils.getFloatLane(zTileDy, triIdx);

                    // Get dimension of bounding box bottom, mid & top segments
                    int bbWidth = IntrinsicUtils.getIntLane(bbTileSizeX, triIdx);
                    int bbHeight = IntrinsicUtils.getIntLane(bbTileSizeY, triIdx);
                    int tileRowIdx = IntrinsicUtils.getIntLane(bbBottomIdx, triIdx);
                    int tileMidRowIdx = IntrinsicUtils.getIntLane(bbMidIdx, triIdx);
                    int tileEndRowIdx = IntrinsicUtils.getIntLane(bbTopIdx, triIdx);

                    if (bbWidth > BIG_TRIANGLE && bbHeight > BIG_TRIANGLE) // For big triangles we use a more expensive but tighter traversal algorithm
                    {
                        if (triMidVtxRight != 0)
                        {
                            RasterizeTriangleSSE(tiles, true, 1, triIdx, bbWidth, tileRowIdx, tileMidRowIdx, tileEndRowIdx, eventStart, slope, slopeTileDelta, zTriMin, zTriMax, ref z0, zx, zy, edgeY, absEdgeX, slopeSign, eventStartRemainder, slopeTileRemainder);
                        }
                        else
                        {
                            RasterizeTriangleSSE(tiles, true, 0, triIdx, bbWidth, tileRowIdx, tileMidRowIdx, tileEndRowIdx, eventStart, slope, slopeTileDelta, zTriMin, zTriMax, ref z0, zx, zy, edgeY, absEdgeX, slopeSign, eventStartRemainder, slopeTileRemainder);
                        }
                    }
                    else
                    {
                        if (triMidVtxRight != 0)
                        {
                            RasterizeTriangleSSE(tiles, false, 1, triIdx, bbWidth, tileRowIdx, tileMidRowIdx, tileEndRowIdx, eventStart, slope, slopeTileDelta, zTriMin, zTriMax, ref z0, zx, zy, edgeY, absEdgeX, slopeSign, eventStartRemainder, slopeTileRemainder);
                        }
                        else
                        {
                            RasterizeTriangleSSE(tiles, false, 0, triIdx, bbWidth, tileRowIdx, tileMidRowIdx, tileEndRowIdx, eventStart, slope, slopeTileDelta, zTriMin, zTriMax, ref z0, zx, zy, edgeY, absEdgeX, slopeSign, eventStartRemainder, slopeTileRemainder);
                        }
                    }
                }
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        void RasterizeTriangleSSE(
            Tile* tiles,
            bool isTightTraversal,
            int midVtxRight,
            uint triIdx,
            int bbWidth,
            int tileRowIdx,
            int tileMidRowIdx,
            int tileEndRowIdx,
            v128* eventStart,
            v128* slope,
            v128* slopeTileDelta,
            v128 zTriMin,
            v128 zTriMax,
            ref v128 z0,
            float zx,
            float zy,
            v128* edgeY,
            v128* absEdgeX,
            v128* slopeSign,
            v128* eventStartRemainder,
            v128* slopeTileRemainder)
        {
            if (X86.Sse4_1.IsSse41Supported)
            {
                const int LEFT_EDGE_BIAS = -1;
                const int RIGHT_EDGE_BIAS = 1;

                v128* triSlopeSign = stackalloc v128[3];
                v128* triSlopeTileDelta = stackalloc v128[3];
                v128* triEdgeY = stackalloc v128[3];
                v128* triSlopeTileRemainder = stackalloc v128[3];
                v128* triEventRemainder = stackalloc v128[3];
                v128* triEvent = stackalloc v128[3];

                for (int i = 0; i < 3; ++i)
                {
                    triSlopeSign[i] = X86.Sse2.set1_epi32(IntrinsicUtils.getIntLane(slopeSign[i], triIdx));
                    triSlopeTileDelta[i] =
                        X86.Sse2.set1_epi32(IntrinsicUtils.getIntLane(slopeTileDelta[i], triIdx));
                    triEdgeY[i] = X86.Sse2.set1_epi32(IntrinsicUtils.getIntLane(edgeY[i], triIdx));
                    triSlopeTileRemainder[i] =
                        X86.Sse2.set1_epi32(IntrinsicUtils.getIntLane(slopeTileRemainder[i], triIdx));

                    v128 triSlope = X86.Sse.set1_ps(IntrinsicUtils.getFloatLane(slope[i], triIdx));
                    v128 triAbsEdgeX = X86.Sse2.set1_epi32(IntrinsicUtils.getIntLane(absEdgeX[i], triIdx));
                    v128 triStartRemainder =
                        X86.Sse2.set1_epi32(IntrinsicUtils.getIntLane(eventStartRemainder[i], triIdx));
                    v128 triEventStart = X86.Sse2.set1_epi32(IntrinsicUtils.getIntLane(eventStart[i], triIdx));

                    v128 SimdLaneYCoordF = X86.Sse.setr_ps(128f, 384f, 640f, 896f);
                    v128 scanlineDelta = X86.Sse2.cvttps_epi32(X86.Sse.mul_ps(triSlope, SimdLaneYCoordF));
                    v128 SimdLaneYCoordI = X86.Sse2.setr_epi32(128, 384, 640, 896);
                    v128 scanlineSlopeRemainder =
                        X86.Sse2.sub_epi32(X86.Sse4_1.mullo_epi32(triAbsEdgeX, SimdLaneYCoordI),
                            X86.Sse4_1.mullo_epi32(X86.Ssse3.abs_epi32(scanlineDelta), triEdgeY[i]));

                    triEventRemainder[i] = X86.Sse2.sub_epi32(triStartRemainder, scanlineSlopeRemainder);
                    v128 overflow = X86.Sse2.srai_epi32(triEventRemainder[i], 31);
                    triEventRemainder[i] =
                        X86.Sse2.add_epi32(triEventRemainder[i], X86.Sse2.and_si128(overflow, triEdgeY[i]));
                    triEvent[i] =
                        X86.Sse2.add_epi32(X86.Sse2.add_epi32(triEventStart, scanlineDelta),
                            X86.Sse2.and_si128(overflow, triSlopeSign[i]));
                }

                // For big triangles track start & end tile for each scanline and only traverse the valid region
                int startDelta = 0;
                int endDelta = 0;
                int topDelta = 0;
                int startEvent = 0;
                int endEvent = 0;
                int topEvent = 0;

                if (isTightTraversal)
                {
                    startDelta = IntrinsicUtils.getIntLane(slopeTileDelta[2], triIdx) + LEFT_EDGE_BIAS;
                    endDelta = IntrinsicUtils.getIntLane(slopeTileDelta[0], triIdx) + RIGHT_EDGE_BIAS;
                    topDelta = IntrinsicUtils.getIntLane(slopeTileDelta[1], triIdx) +
                                (midVtxRight != 0 ? RIGHT_EDGE_BIAS : LEFT_EDGE_BIAS);

                    // Compute conservative bounds for the edge events over a 32xN tile
                    startEvent = IntrinsicUtils.getIntLane(eventStart[2], triIdx) + Mathf.Min(0, startDelta);
                    endEvent = IntrinsicUtils.getIntLane(eventStart[0], triIdx) + Mathf.Max(0, endDelta) +
                                (BufferGroup.TileWidth << BufferGroup.FpBits); // TODO: (Apoorva) can be spun out into a const

                    if (midVtxRight != 0)
                    {
                        topEvent = IntrinsicUtils.getIntLane(eventStart[1], triIdx) + Mathf.Max(0, topDelta) +
                                    (BufferGroup.TileWidth << BufferGroup.FpBits); // TODO: (Apoorva) can be spun out into a const
                    }
                    else
                    {
                        topEvent = IntrinsicUtils.getIntLane(eventStart[1], triIdx) + Mathf.Min(0, topDelta);
                    }
                }

                if (tileRowIdx <= tileMidRowIdx)
                {
                    int tileStopIdx = Mathf.Min(tileEndRowIdx, tileMidRowIdx);

                    // Traverse the bottom half of the triangle
                    while (tileRowIdx < tileStopIdx)
                    {
                        int start = 0;
                        int end = bbWidth;

                        if (isTightTraversal)
                        {
                            // Compute tighter start and endpoints to avoid traversing empty space
                            start = Mathf.Max(0, Mathf.Min(bbWidth - 1, startEvent >> (BufferGroup.TileWidthShift + BufferGroup.FpBits))); // TODO: (Apoorva) can be spun out into a const
                            end = Mathf.Min(bbWidth, ((int)endEvent >> (BufferGroup.TileWidthShift + BufferGroup.FpBits))); // TODO: (Apoorva) can be spun out into a const

                            startEvent += startDelta;
                            endEvent += endDelta;
                        }

                        // Traverse the scanline and update the masked hierarchical z buffer
                        TraverseScanlineSSE(tiles, 1, 1, start, end, tileRowIdx, 0, 2, triEvent, zTriMin, zTriMax, z0,
                            zx);

                        // move to the next scanline of tiles, update edge events and interpolate z
                        tileRowIdx += NumTilesX;
                        z0 = X86.Sse.add_ps(z0, X86.Sse.set1_ps(zy));

                        UpdateTileEventsYSSE(triEventRemainder, triSlopeTileRemainder, triEdgeY, triEvent,
                            triSlopeTileDelta, triSlopeSign, 0);
                        UpdateTileEventsYSSE(triEventRemainder, triSlopeTileRemainder, triEdgeY, triEvent,
                            triSlopeTileDelta, triSlopeSign, 2);
                    }

                    // Traverse the middle scanline of tiles. We must consider all three edges only in this region
                    if (tileRowIdx < tileEndRowIdx)
                    {
                        int start = 0;
                        int end = bbWidth;

                        if (isTightTraversal)
                        {
                            // Compute tighter start and endpoints to avoid traversing lots of empty space
                            start = Mathf.Max(0, Mathf.Min(bbWidth - 1, startEvent >> (BufferGroup.TileWidthShift + BufferGroup.FpBits))); // TODO: (Apoorva) can be spun out into a const
                            end = Mathf.Min(bbWidth, ((int)endEvent >> (BufferGroup.TileWidthShift + BufferGroup.FpBits))); // TODO: (Apoorva) can be spun out into a const

                            // Switch the traversal start / end to account for the upper side edge
                            endEvent = midVtxRight != 0 ? topEvent : endEvent;
                            endDelta = midVtxRight != 0 ? topDelta : endDelta;
                            startEvent = midVtxRight != 0 ? startEvent : topEvent;
                            startDelta = midVtxRight != 0 ? startDelta : topDelta;

                            startEvent += startDelta;
                            endEvent += endDelta;
                        }

                        // Traverse the scanline and update the masked hierarchical z buffer.
                        if (midVtxRight != 0)
                        {
                            TraverseScanlineSSE(tiles, 2, 1, start, end, tileRowIdx, 0, 2, triEvent, zTriMin, zTriMax,
                                z0, zx);
                        }
                        else
                        {
                            TraverseScanlineSSE(tiles, 1, 2, start, end, tileRowIdx, 0, 2, triEvent, zTriMin, zTriMax,
                                z0, zx);
                        }

                        tileRowIdx += NumTilesX;
                    }

                    // Traverse the top half of the triangle
                    if (tileRowIdx < tileEndRowIdx)
                    {
                        // move to the next scanline of tiles, update edge events and interpolate z
                        z0 = X86.Sse.add_ps(z0, X86.Sse.set1_ps(zy));
                        int i0 = midVtxRight + 0;
                        int i1 = midVtxRight + 1;

                        UpdateTileEventsYSSE(triEventRemainder, triSlopeTileRemainder, triEdgeY, triEvent,
                            triSlopeTileDelta, triSlopeSign, i0);
                        UpdateTileEventsYSSE(triEventRemainder, triSlopeTileRemainder, triEdgeY, triEvent,
                            triSlopeTileDelta, triSlopeSign, i1);

                        for (; ; )
                        {
                            int start = 0;
                            int end = bbWidth;

                            if (isTightTraversal)
                            {
                                // Compute tighter start and endpoints to avoid traversing lots of empty space
                                start = Mathf.Max(0, Mathf.Min(bbWidth - 1, startEvent >> (BufferGroup.TileWidthShift + BufferGroup.FpBits)));
                                end = Mathf.Min(bbWidth, (endEvent >> (BufferGroup.TileWidthShift + BufferGroup.FpBits)));

                                startEvent += startDelta;
                                endEvent += endDelta;
                            }

                            // Traverse the scanline and update the masked hierarchical z buffer
                            TraverseScanlineSSE(tiles, 1, 1, start, end, tileRowIdx, midVtxRight + 0,
                                midVtxRight + 1, triEvent, zTriMin, zTriMax, z0, zx);

                            // move to the next scanline of tiles, update edge events and interpolate z
                            tileRowIdx += NumTilesX;
                            if (tileRowIdx >= tileEndRowIdx)
                            {
                                break;
                            }

                            z0 = X86.Sse.add_ps(z0, X86.Sse.set1_ps(zy));

                            UpdateTileEventsYSSE(triEventRemainder, triSlopeTileRemainder, triEdgeY, triEvent,
                                triSlopeTileDelta, triSlopeSign, i0);
                            UpdateTileEventsYSSE(triEventRemainder, triSlopeTileRemainder, triEdgeY, triEvent,
                                triSlopeTileDelta, triSlopeSign, i1);
                        }
                    }
                }
                else
                {
                    if (isTightTraversal)
                    {
                        // For large triangles, switch the traversal start / end to account for the upper side edge
                        endEvent = midVtxRight != 0 ? topEvent : endEvent;
                        endDelta = midVtxRight != 0 ? topDelta : endDelta;
                        startEvent = midVtxRight != 0 ? startEvent : topEvent;
                        startDelta = midVtxRight != 0 ? startDelta : topDelta;
                    }

                    // Traverse the top half of the triangle
                    if (tileRowIdx < tileEndRowIdx)
                    {
                        int i0 = midVtxRight + 0;
                        int i1 = midVtxRight + 1;

                        for (; ; )
                        {
                            int start = 0;
                            int end = bbWidth;

                            if (isTightTraversal)
                            {
                                // Compute tighter start and endpoints to avoid traversing lots of empty space
                                start = Mathf.Max(0, Mathf.Min(bbWidth - 1, startEvent >> (BufferGroup.TileWidthShift + BufferGroup.FpBits)));
                                end = Mathf.Min(bbWidth, (endEvent >> (BufferGroup.TileWidthShift + BufferGroup.FpBits)));

                                startEvent += startDelta;
                                endEvent += endDelta;
                            }

                            // Traverse the scanline and update the masked hierarchical z buffer
                            TraverseScanlineSSE(tiles, 1, 1, start, end, tileRowIdx, midVtxRight + 0,
                                midVtxRight + 1, triEvent, zTriMin, zTriMax, z0, zx);

                            // move to the next scanline of tiles, update edge events and interpolate z
                            tileRowIdx += NumTilesX;
                            if (tileRowIdx >= tileEndRowIdx)
                            {
                                break;
                            }

                            z0 = X86.Sse.add_ps(z0, X86.Sse.set1_ps(zy));

                            UpdateTileEventsYSSE(triEventRemainder, triSlopeTileRemainder, triEdgeY, triEvent,
                                triSlopeTileDelta, triSlopeSign, i0);
                            UpdateTileEventsYSSE(triEventRemainder, triSlopeTileRemainder, triEdgeY, triEvent,
                                triSlopeTileDelta, triSlopeSign, i1);
                        }
                    }
                }
            }
        }
        #endregion

        #region Neon
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void TraverseScanlineNEON(Tile* tiles, int numRight, int numLeft, int leftOffset, int rightOffset, int tileIdx, int rightEvent, int leftEvent, v128* events, v128 zTriMin, v128 zTriMax, v128 iz0, float zx)
        {
            if (Arm.Neon.IsNeonSupported)
            {
                v128* right = stackalloc v128[numRight];
                v128* left = stackalloc v128[numLeft];

                // Floor edge events to integer pixel coordinates (shift out fixed point bits)
                v128 eventOffset = new v128(leftOffset << BufferGroup.TileWidthShift);
                v128 emptyBitMask = new v128(0);
                v128 fullBitMask = new v128(~0);
                v128 highbit = new v128(0x80000000u);
                v128 simdTileWidth = new v128(BufferGroup.TileWidth);

                for (int i = 0; i < numRight; ++i)
                {
                    right[i] = Arm.Neon.vmaxq_s32(Arm.Neon.vsubq_s32(Arm.Neon.vshrq_n_s32(events[rightEvent + i], BufferGroup.FpBits), eventOffset), emptyBitMask);

                }

                for (int i = 0; i < numLeft; ++i)
                {
                    left[i] = Arm.Neon.vmaxq_s32(Arm.Neon.vsubq_s32(Arm.Neon.vshrq_n_s32(events[leftEvent - i], BufferGroup.FpBits), eventOffset), emptyBitMask);
                }

                v128 z0 = Arm.Neon.vaddq_f32(iz0, new v128(zx * leftOffset));
                int tileIdxEnd = tileIdx + rightOffset;
                tileIdx += leftOffset;

                for (; ; )
                {
                    // Compute zMin for the overlapped layers
                    v128 mask = tiles[tileIdx].mask;
                    v128 zMin0 = IntrinsicUtils._vblendq_f32(Arm.Neon.vceqq_s32(mask, fullBitMask), tiles[tileIdx].zMin0, tiles[tileIdx].zMin1);
                    v128 zMin1 = IntrinsicUtils._vblendq_f32(Arm.Neon.vceqq_s32(mask, emptyBitMask), tiles[tileIdx].zMin1, tiles[tileIdx].zMin0);
                    v128 zMinBuf = Arm.Neon.vminq_f32(zMin0, zMin1);
                    v128 comp = Arm.Neon.vcltq_f32(zTriMax, zMinBuf);
                    // https://community.arm.com/arm-community-blogs/b/infrastructure-solutions-blog/posts/porting-x86-vector-bitmask-optimizations-to-arm-neon
                    // Instead of movemask_ps
                    v64 compPacked = Arm.Neon.vshrn_n_s32(comp, 16);

                    if (compPacked.ULong0 != 0xfffffffffffffffful)
                    {
                        // Compute coverage mask for entire 32xN using shift operations
                        v128 accumulatedMask = IntrinsicUtils._vsllv_ones(left[0]);

                        for (int i = 1; i < numLeft; ++i)
                        {
                            accumulatedMask = Arm.Neon.vandq_s8(accumulatedMask, IntrinsicUtils._vsllv_ones(left[i]));
                        }

                        for (int i = 0; i < numRight; ++i)
                        {
                            accumulatedMask = Arm.Neon.vbicq_s8(accumulatedMask, IntrinsicUtils._vsllv_ones(right[i]));
                        }

                        // Compute interpolated min for each 8x4 subtile and update the masked hierarchical z buffer entry
                        v128 zSubTileMin = Arm.Neon.vmaxq_f32(z0, zTriMin);
                        UpdateTileAccurateNEON(tiles, tileIdx, IntrinsicUtils._vtranspose_s8(accumulatedMask), zSubTileMin);
                    }

                    // Update buffer address, interpolate z and edge events
                    tileIdx++;

                    if (tileIdx >= tileIdxEnd)
                    {
                        break;
                    }

                    z0 = Arm.Neon.vaddq_f32(z0, new v128(zx));

                    for (int i = 0; i < numRight; ++i)
                    {
                        // Trick, use sub saturated to avoid checking against < 0 for shift (values should fit in 16 bits)
                        right[i] = Arm.Neon.vqsubq_u16(right[i], simdTileWidth);
                    }

                    for (int i = 0; i < numLeft; ++i)
                    {
                        // Trick, use sub saturated to avoid checking against < 0 for shift (values should fit in 16 bits)
                        left[i] = Arm.Neon.vqsubq_u16(left[i], simdTileWidth);
                    }
                }
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        void UpdateTileAccurateNEON(Tile* tiles, int tileIdx, v128 coverage, v128 zTriv)
        {
            if (Arm.Neon.IsNeonSupported)
            {
                v128 zMin0 = tiles[tileIdx].zMin0;
                v128 zMin1 = tiles[tileIdx].zMin1;
                v128 mask = tiles[tileIdx].mask;

                // Swizzle coverage mask to 8x4 subtiles
                v128 rastMask = coverage;

                // Perform individual depth tests with layer 0 & 1 and mask out all failing pixels
                v128 sdist0 = Arm.Neon.vsubq_f32(zMin0, zTriv);
                v128 sdist1 = Arm.Neon.vsubq_f32(zMin1, zTriv);
                v128 sign0 = Arm.Neon.vshrq_n_s32(sdist0, 31);
                v128 sign1 = Arm.Neon.vshrq_n_s32(sdist1, 31);
                v128 triMask = Arm.Neon.vandq_s8(rastMask, Arm.Neon.vorrq_s8(Arm.Neon.vbicq_s8(sign0, mask), Arm.Neon.vandq_s8(mask, sign1)));
                // Early out if no pixels survived the depth test
                // (this test is more accurate than the early culling test in TraverseScanline())
                v64 narrowSaturatedMask = Arm.Neon.vqmovn_u64(triMask);
                if (narrowSaturatedMask.ULong0 == 0ul)
                {
                    return;
                }

#if MOC_ENABLE_STATS
                STATS_ADD(ref mStats.mOccluders.mNumTilesUpdated, 1);
#endif
                v128 t0 = Arm.Neon.vceqzq_s32(triMask);
                v128 zTri = IntrinsicUtils._vblendq_f32(t0, zTriv, zMin0);

                // Test if incoming triangle completely overwrites layer 0 or 1
                v128 layerMask0 = Arm.Neon.vbicq_s8(Arm.Neon.vmvnq_s32(mask), triMask);
                v128 layerMask1 = Arm.Neon.vbicq_s8(mask, triMask);
                v128 lm0 = Arm.Neon.vceqzq_s32(layerMask0);
                v128 lm1 = Arm.Neon.vceqzq_s32(layerMask1);
                v128 z0 = IntrinsicUtils._vblendq_f32(lm0, zMin0, zTri);
                v128 z1 = IntrinsicUtils._vblendq_f32(lm1, zMin1, zTri);

                // Compute distances used for merging heuristic
                v128 d0 = Arm.Neon.vabsq_f32(sdist0);
                v128 d1 = Arm.Neon.vabsq_f32(sdist1);
                v128 d2 = Arm.Neon.vabdq_f32(z0, z1);

                // Find minimum distance
                v128 c01 = Arm.Neon.vsubq_f32(d0, d1);
                v128 c02 = Arm.Neon.vsubq_f32(d0, d2);
                v128 c12 = Arm.Neon.vsubq_f32(d1, d2);

                // Two tests indicating which layer the incoming triangle will merge with or
                // overwrite. d0min indicates that the triangle will overwrite layer 0, and
                // d1min flags that the triangle will overwrite layer 1.
                v128 d0min = Arm.Neon.vorrq_s8(Arm.Neon.vandq_s8(c01, c02), Arm.Neon.vorrq_s8(lm0, t0));
                v128 d1min = Arm.Neon.vbicq_s8(Arm.Neon.vorrq_s8(c12, lm1), d0min);

                /* Update depth buffer entry. NOTE: we always merge into layer 0, so if the
                   triangle should be merged with layer 1, we first swap layer 0 & 1 and then
                   merge into layer 0. */

                // Update mask based on which layer the triangle overwrites or was merged into
                v128 inner = IntrinsicUtils._vblendq_f32(d0min, triMask, layerMask1);

                // Update the zMin[0] value. There are four outcomes: overwrite with layer 1,
                // merge with layer 1, merge with zTri or overwrite with layer 1 and then merge
                // with zTri.
                v128 e0 = IntrinsicUtils._vblendq_f32(d1min, z0, z1);
                v128 e1 = IntrinsicUtils._vblendq_f32(Arm.Neon.vorrq_s8(d1min, d0min), z1, zTri);

                // Update the zMin[1] value. There are three outcomes: keep current value,
                // overwrite with zTri, or overwrite with z1
                v128 z1t = IntrinsicUtils._vblendq_f32(d0min, zTri, z1);

                tiles[tileIdx].zMin0 = Arm.Neon.vminq_f32(e0, e1);
                tiles[tileIdx].zMin1 = IntrinsicUtils._vblendq_f32(d1min, z1t, z0);
                tiles[tileIdx].mask = IntrinsicUtils._vblendq_f32(d1min, inner, layerMask0);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void UpdateTileEventsYNEON(v128* triEventRemainder, v128* triSlopeTileRemainder, v128* triEdgeY, v128* triEvent, v128* triSlopeTileDelta, v128* triSlopeSign, int i)
        {
            if (Arm.Neon.IsNeonSupported)
            {
                triEventRemainder[i] = Arm.Neon.vsubq_s32(triEventRemainder[i], triSlopeTileRemainder[i]);
                v128 overflow = Arm.Neon.vshrq_n_s32(triEventRemainder[i], 31);
                triEventRemainder[i] = Arm.Neon.vaddq_s32(triEventRemainder[i], Arm.Neon.vandq_s8(overflow, triEdgeY[i]));
                triEvent[i] = Arm.Neon.vaddq_s32(triEvent[i], Arm.Neon.vaddq_s32(triSlopeTileDelta[i], Arm.Neon.vandq_s8(overflow, triSlopeSign[i])));
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void SortVerticesNEON(v128* vX, v128* vY)
        {
            if (Arm.Neon.IsNeonSupported)
            {
                // Rotate the triangle in the winding order until v0 is the vertex with lowest Y value
                for (int i = 0; i < 2; i++)
                {
                    v128 ey1 = Arm.Neon.vsubq_s32(vY[1], vY[0]);
                    v128 ey2 = Arm.Neon.vsubq_s32(vY[2], vY[0]);
                    v128 swapMask = Arm.Neon.vorrq_s8(Arm.Neon.vorrq_s8(ey1, ey2), Arm.Neon.vceqzq_s32(ey2));

                    v128 sX = IntrinsicUtils._vblendq_f32(swapMask, vX[2], vX[0]);
                    vX[0] = IntrinsicUtils._vblendq_f32(swapMask, vX[0], vX[1]);
                    vX[1] = IntrinsicUtils._vblendq_f32(swapMask, vX[1], vX[2]);
                    vX[2] = sX;

                    v128 sY = IntrinsicUtils._vblendq_f32(swapMask, vY[2], vY[0]);
                    vY[0] = IntrinsicUtils._vblendq_f32(swapMask, vY[0], vY[1]);
                    vY[1] = IntrinsicUtils._vblendq_f32(swapMask, vY[1], vY[2]);
                    vY[2] = sY;
                }
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void ComputeDepthPlaneNEON(v128* pVtxX, v128* pVtxY, v128* pVtxZ, out v128 zPixelDx, out v128 zPixelDy)
        {
            if (Arm.Neon.IsNeonSupported)
            {
                // Setup z(x,y) = z0 + dx*x + dy*y screen space depth plane equation
                v128 x2 = Arm.Neon.vsubq_f32(pVtxX[2], pVtxX[0]);
                v128 x1 = Arm.Neon.vsubq_f32(pVtxX[1], pVtxX[0]);
                v128 y1 = Arm.Neon.vsubq_f32(pVtxY[1], pVtxY[0]);
                v128 y2 = Arm.Neon.vsubq_f32(pVtxY[2], pVtxY[0]);
                v128 z1 = Arm.Neon.vsubq_f32(pVtxZ[1], pVtxZ[0]);
                v128 z2 = Arm.Neon.vsubq_f32(pVtxZ[2], pVtxZ[0]);
                v128 d = Arm.Neon.vdivq_f32(new v128(1.0f), Arm.Neon.vmlsq_f32(Arm.Neon.vmulq_f32(x1, y2), y1, x2));
                zPixelDx = Arm.Neon.vmulq_f32(Arm.Neon.vmlsq_f32(Arm.Neon.vmulq_f32(z1, y2), y1, z2), d);
                zPixelDy = Arm.Neon.vmulq_f32(Arm.Neon.vmlsq_f32(Arm.Neon.vmulq_f32(x1, z2), z1, x2), d);
            }
            else
            {
                zPixelDx = new v128();
                zPixelDy = new v128();
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void ComputeBoundingBoxNEON(v128* vX, v128* vY, ref ScissorRect scissor, out v128 bbminX, out v128 bbminY, out v128 bbmaxX, out v128 bbmaxY)
        {
            if (Arm.Neon.IsNeonSupported)
            {
                // Find Min/Max vertices
                bbminX = Arm.Neon.vcvtnq_s32_f32(Arm.Neon.vminq_f32(vX[0], Arm.Neon.vminq_f32(vX[1], vX[2])));
                bbminY = Arm.Neon.vcvtnq_s32_f32(Arm.Neon.vminq_f32(vY[0], Arm.Neon.vminq_f32(vY[1], vY[2])));
                bbmaxX = Arm.Neon.vcvtnq_s32_f32(Arm.Neon.vmaxq_f32(vX[0], Arm.Neon.vmaxq_f32(vX[1], vX[2])));
                bbmaxY = Arm.Neon.vcvtnq_s32_f32(Arm.Neon.vmaxq_f32(vY[0], Arm.Neon.vmaxq_f32(vY[1], vY[2])));

                // Clamp to tile boundaries
                v128 SimdPadWMask = new v128(~(BufferGroup.TileWidth - 1));
                v128 SimdPadHMask = new v128(~(BufferGroup.TileHeight - 1));
                bbminX = Arm.Neon.vandq_s8(bbminX, SimdPadWMask);
                bbmaxX = Arm.Neon.vandq_s8(Arm.Neon.vaddq_s32(bbmaxX, new v128(BufferGroup.TileWidth)), SimdPadWMask);
                bbminY = Arm.Neon.vandq_s8(bbminY, SimdPadHMask);
                bbmaxY = Arm.Neon.vandq_s8(Arm.Neon.vaddq_s32(bbmaxY, new v128(BufferGroup.TileHeight)), SimdPadHMask);

                // Clip to scissor
                bbminX = Arm.Neon.vmaxq_s32(bbminX, new v128(scissor.mMinX));
                bbmaxX = Arm.Neon.vminq_s32(bbmaxX, new v128(scissor.mMaxX));
                bbminY = Arm.Neon.vmaxq_s32(bbminY, new v128(scissor.mMinY));
                bbmaxY = Arm.Neon.vminq_s32(bbmaxY, new v128(scissor.mMaxY));
            }
            else
            {
                bbminX = new v128();
                bbminY = new v128();
                bbmaxX = new v128();
                bbmaxY = new v128();
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        void ProjectVerticesNEON(v128* ipVtxX, v128* ipVtxY, v128* pVtxX, v128* pVtxY, v128* pVtxZ, v128* vtxX, v128* vtxY, v128* vtxW)
        {
            if (Arm.Neon.IsNeonSupported)
            {
                const float FP_INV = 1f / (1 << BufferGroup.FpBits);
                // Project vertices and transform to screen space. Snap to sub-pixel coordinates with BufferGroup.FpBits precision.
                for (int i = 0; i < 3; i++)
                {
                    int idx = 2 - i;
                    v128 rcpW;

                    if (ProjectionType == BatchCullingProjectionType.Orthographic)
                    {
                        rcpW = Arm.Neon.vmlaq_f32(new v128(1.0f), vtxW[i], new v128(-1.0f));

                        v128 screenX = Arm.Neon.vmlaq_f32(PixelCenterX, vtxX[i], HalfWidth);
                        v128 screenY = Arm.Neon.vmlaq_f32(PixelCenterY, vtxY[i], HalfHeight);
                        ipVtxX[idx] = Arm.Neon.vcvtnq_s32_f32(Arm.Neon.vmulq_f32(screenX, new v128((float)(1 << BufferGroup.FpBits))));
                        ipVtxY[idx] = Arm.Neon.vcvtnq_s32_f32(Arm.Neon.vmulq_f32(screenY, new v128((float)(1 << BufferGroup.FpBits))));

                    }
                    else
                    {
                        rcpW = Arm.Neon.vdivq_f32(new v128(1.0f), vtxW[i]);

                        v128 screenX = Arm.Neon.vmlaq_f32(PixelCenterX, Arm.Neon.vmulq_f32(vtxX[i], HalfWidth), rcpW);
                        v128 screenY = Arm.Neon.vmlaq_f32(PixelCenterY, Arm.Neon.vmulq_f32(vtxY[i], HalfHeight), rcpW);

                        ipVtxX[idx] = Arm.Neon.vcvtnq_s32_f32(Arm.Neon.vmulq_f32(screenX, new v128((float)(1 << BufferGroup.FpBits))));
                        ipVtxY[idx] = Arm.Neon.vcvtnq_s32_f32(Arm.Neon.vmulq_f32(screenY, new v128((float)(1 << BufferGroup.FpBits))));
                    }

                    pVtxX[idx] = Arm.Neon.vmulq_f32(Arm.Neon.vcvtq_f32_s32(ipVtxX[idx]), new v128(FP_INV));
                    pVtxY[idx] = Arm.Neon.vmulq_f32(Arm.Neon.vcvtq_f32_s32(ipVtxY[idx]), new v128(FP_INV));
                    pVtxZ[idx] = rcpW;
                }
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void RasterizeTriangleBatchNEON(Tile* tiles, v128* ipVtxX, v128* ipVtxY, v128* pVtxX, v128* pVtxY, v128* pVtxZ, uint triMask, ScissorRect scissor)
        {
            if (Arm.Neon.IsNeonSupported)
            {
                v128 one = new v128(1);
                v128 zero = new v128(0);
                //we are computing the bounding box again when we used it before but there are some use cases after, this check cannot be removed atm

                // Compute bounding box and clamp to tile coordinates
                ComputeBoundingBoxNEON(pVtxX, pVtxY, ref scissor, out var bbPixelMinX, out var bbPixelMinY, out var bbPixelMaxX, out var bbPixelMaxY);

                // Clamp bounding box to tiles (it's already padded in computeBoundingBox)
                v128 bbTileMinX = Arm.Neon.vshrq_n_s32(bbPixelMinX, BufferGroup.TileWidthShift);
                v128 bbTileMinY = Arm.Neon.vshrq_n_s32(bbPixelMinY, BufferGroup.TileHeightShift);
                v128 bbTileMaxX = Arm.Neon.vshrq_n_s32(bbPixelMaxX, BufferGroup.TileWidthShift);
                v128 bbTileMaxY = Arm.Neon.vshrq_n_s32(bbPixelMaxY, BufferGroup.TileHeightShift);
                v128 bbTileSizeX = Arm.Neon.vsubq_s32(bbTileMaxX, bbTileMinX);
                v128 bbTileSizeY = Arm.Neon.vsubq_s32(bbTileMaxY, bbTileMinY);

                // Cull triangles with zero bounding box
                v128 bboxSign = Arm.Neon.vorrq_s8(Arm.Neon.vsubq_s32(bbTileSizeX, one), Arm.Neon.vsubq_s32(bbTileSizeY, one));
                triMask &= (uint)((~IntrinsicUtils._vmovemask_f32(bboxSign)) & SIMD_ALL_LANES_MASK);

                if (triMask == 0x0)
                {
                    return; // View-culled
                }

                // Set up screen space depth plane
                ComputeDepthPlaneNEON(pVtxX, pVtxY, pVtxZ, out var zPixelDx, out var zPixelDy);

                // Compute z value at min corner of bounding box. Offset to make sure z is conservative for all 8x4 subtiles
                v128 bbMinXV0 = Arm.Neon.vsubq_f32(Arm.Neon.vcvtq_f32_s32(bbPixelMinX), pVtxX[0]);
                v128 bbMinYV0 = Arm.Neon.vsubq_f32(Arm.Neon.vcvtq_f32_s32(bbPixelMinY), pVtxY[0]);
                v128 zPlaneOffset = Arm.Neon.vmlaq_f32(
                    Arm.Neon.vmlaq_f32(pVtxZ[0], zPixelDy, bbMinYV0),
                    zPixelDx,
                    bbMinXV0);
                v128 zTileDx = Arm.Neon.vmulq_f32(zPixelDx, new v128((float)BufferGroup.TileWidth));
                v128 zTileDy = Arm.Neon.vmulq_f32(zPixelDy, new v128((float)BufferGroup.TileHeight));

                zPlaneOffset = Arm.Neon.vaddq_f32(zPlaneOffset, Arm.Neon.vminq_f32(zero, Arm.Neon.vmulq_f32(zPixelDx, new v128((float)BufferGroup.SubTileWidth))));
                zPlaneOffset = Arm.Neon.vaddq_f32(zPlaneOffset, Arm.Neon.vminq_f32(zero, Arm.Neon.vmulq_f32(zPixelDy, new v128((float)BufferGroup.SubTileHeight))));

                // Compute Zmin and Zmax for the triangle (used to narrow the range for difficult tiles)
                v128 zMin = Arm.Neon.vminq_f32(pVtxZ[0], Arm.Neon.vminq_f32(pVtxZ[1], pVtxZ[2]));
                v128 zMax = Arm.Neon.vmaxq_f32(pVtxZ[0], Arm.Neon.vmaxq_f32(pVtxZ[1], pVtxZ[2]));

                /* Sort vertices (v0 has lowest Y, and the rest is in winding order) and compute edges. Also find the middle
                    vertex and compute tile */

                // Rotate the triangle in the winding order until v0 is the vertex with lowest Y value
                SortVerticesNEON(ipVtxX, ipVtxY);

                // Compute edges
                v128* edgeX = stackalloc v128[3];
                edgeX[0] = Arm.Neon.vsubq_s32(ipVtxX[1], ipVtxX[0]);
                edgeX[1] = Arm.Neon.vsubq_s32(ipVtxX[2], ipVtxX[1]);
                edgeX[2] = Arm.Neon.vsubq_s32(ipVtxX[2], ipVtxX[0]);

                v128* edgeY = stackalloc v128[3];
                edgeY[0] = Arm.Neon.vsubq_s32(ipVtxY[1], ipVtxY[0]);
                edgeY[1] = Arm.Neon.vsubq_s32(ipVtxY[2], ipVtxY[1]);
                edgeY[2] = Arm.Neon.vsubq_s32(ipVtxY[2], ipVtxY[0]);

                // Classify if the middle vertex is on the left or right and compute its position
                int midVtxRight = ~IntrinsicUtils._vmovemask_f32(edgeY[1]);
                v128 midPixelX = IntrinsicUtils._vblendq_f32(edgeY[1], ipVtxX[1], ipVtxX[2]);
                v128 midPixelY = IntrinsicUtils._vblendq_f32(edgeY[1], ipVtxY[1], ipVtxY[2]);
                v128 midTileY = Arm.Neon.vshrq_n_s32(Arm.Neon.vmaxq_s32(midPixelY, zero), BufferGroup.TileHeightShift + BufferGroup.FpBits);
                v128 bbMidTileY = Arm.Neon.vmaxq_s32(bbTileMinY, Arm.Neon.vminq_s32(bbTileMaxY, midTileY));

                // Compute edge events for the bottom of the bounding box, or for the middle tile in case of
                // the edge originating from the middle vertex.
                v128* xDiffi = stackalloc v128[2];
                xDiffi[0] = Arm.Neon.vsubq_s32(ipVtxX[0], Arm.Neon.vshlq_n_s32(bbPixelMinX, BufferGroup.FpBits));
                xDiffi[1] = Arm.Neon.vsubq_s32(midPixelX, Arm.Neon.vshlq_n_s32(bbPixelMinX, BufferGroup.FpBits));

                v128* yDiffi = stackalloc v128[2];
                yDiffi[0] = Arm.Neon.vsubq_s32(ipVtxY[0], Arm.Neon.vshlq_n_s32(bbPixelMinY, BufferGroup.FpBits));
                yDiffi[1] = Arm.Neon.vsubq_s32(midPixelY, Arm.Neon.vshlq_n_s32(bbMidTileY, BufferGroup.FpBits + BufferGroup.TileHeightShift));

                /* Edge slope setup - Note we do not conform to DX/GL rasterization rules */

                // Potentially flip edge to ensure that all edges have positive Y slope.
                edgeX[1] = IntrinsicUtils._vblendq_f32(edgeY[1], edgeX[1], Arm.Neon.vnegq_s32(edgeX[1]));
                edgeY[1] = Arm.Neon.vabsq_s32(edgeY[1]);

                // Compute floating point slopes
                v128* slope = stackalloc v128[3];
                slope[0] = Arm.Neon.vdivq_f32(Arm.Neon.vcvtq_f32_s32(edgeX[0]), Arm.Neon.vcvtq_f32_s32(edgeY[0]));
                slope[1] = Arm.Neon.vdivq_f32(Arm.Neon.vcvtq_f32_s32(edgeX[1]), Arm.Neon.vcvtq_f32_s32(edgeY[1]));
                slope[2] = Arm.Neon.vdivq_f32(Arm.Neon.vcvtq_f32_s32(edgeX[2]), Arm.Neon.vcvtq_f32_s32(edgeY[2]));

                // Modify slope of horizontal edges to make sure they mask out pixels above/below the edge. The slope is set to screen
                // width to mask out all pixels above or below the horizontal edge. We must also add a small bias to acount for that
                // vertices may end up off screen due to clipping. We're assuming that the round off error is no bigger than 1.0
                v128 horizontalSlopeDelta = new v128(2f * (NumPixelsX + 2f * (BufferGroup.GuardBandPixelSize + 1.0f)));
                v128 horizontalSlope0 = Arm.Neon.vceqzq_s32(edgeY[0]);
                v128 horizontalSlope1 = Arm.Neon.vceqzq_s32(edgeY[1]);
                slope[0] = IntrinsicUtils._vblendq_f32(horizontalSlope0, slope[0], horizontalSlopeDelta);
                slope[1] = IntrinsicUtils._vblendq_f32(horizontalSlope1, slope[1], Arm.Neon.vnegq_f32(horizontalSlopeDelta));

                v128* vy = stackalloc v128[3];
                vy[0] = yDiffi[0];
                vy[1] = yDiffi[1];
                vy[2] = yDiffi[0];

                v128 offset0 = Arm.Neon.vandq_s8(Arm.Neon.vaddq_s32(yDiffi[0], new v128(BufferGroup.FpHalfPixel - 1)), new v128((-1 << BufferGroup.FpBits)));
                v128 offset1 = Arm.Neon.vandq_s8(Arm.Neon.vaddq_s32(yDiffi[1], new v128(BufferGroup.FpHalfPixel - 1)), new v128((-1 << BufferGroup.FpBits)));
                vy[0] = IntrinsicUtils._vblendq_f32(horizontalSlope0, yDiffi[0], offset0);
                vy[1] = IntrinsicUtils._vblendq_f32(horizontalSlope1, yDiffi[1], offset1);

                // Compute edge events for the bottom of the bounding box, or for the middle tile in case of
                // the edge originating from the middle vertex.
                v128* slopeSign = stackalloc v128[3];
                v128* absEdgeX = stackalloc v128[3];
                v128* slopeTileDelta = stackalloc v128[3];
                v128* eventStartRemainder = stackalloc v128[3];
                v128* slopeTileRemainder = stackalloc v128[3];
                v128* eventStart = stackalloc v128[3];

                for (int i = 0; i < 3; i++)
                {
                    // Common, compute slope sign (used to propagate the remainder term when overflowing) is postive or negative x-direction
                    slopeSign[i] = IntrinsicUtils._vblendq_f32(edgeX[i], new v128(1), new v128(-1));
                    absEdgeX[i] = Arm.Neon.vabsq_s32(edgeX[i]);

                    // Delta and error term for one vertical tile step. The exact delta is exactDelta = edgeX / edgeY, due to limited precision we
                    // repersent the delta as delta = qoutient + remainder / edgeY, where quotient = int(edgeX / edgeY). In this case, since we step
                    // one tile of scanlines at a time, the slope is computed for a tile-sized step.
                    slopeTileDelta[i] = Arm.Neon.vcvtnq_s32_f32(Arm.Neon.vmulq_f32(slope[i], new v128((float)BufferGroup.FpTileHeight)));
                    slopeTileRemainder[i] = Arm.Neon.vsubq_s32(Arm.Neon.vshlq_n_s32(absEdgeX[i], BufferGroup.FpTileHeightShift), Arm.Neon.vmulq_u32(Arm.Neon.vabsq_s32(slopeTileDelta[i]), edgeY[i]));

                    // Jump to bottom scanline of tile row, this is the bottom of the bounding box, or the middle vertex of the triangle.
                    // The jump can be in both positive and negative y-direction due to clipping / offscreen vertices.
                    v128 tileStartDir = IntrinsicUtils._vblendq_f32(vy[i], slopeSign[i], Arm.Neon.vnegq_s32(slopeSign[i]));
                    v128 tieBreaker = IntrinsicUtils._vblendq_f32(tileStartDir, zero, one);
                    v128 tileStartSlope = Arm.Neon.vcvtnq_s32_f32(Arm.Neon.vmulq_f32(slope[i], Arm.Neon.vcvtq_f32_s32(Arm.Neon.vnegq_s32(vy[i]))));
                    v128 tileStartRemainder = Arm.Neon.vsubq_s32(Arm.Neon.vmulq_u32(absEdgeX[i], Arm.Neon.vabsq_s32(vy[i])), Arm.Neon.vmulq_u32(Arm.Neon.vabsq_s32(tileStartSlope), edgeY[i]));

                    eventStartRemainder[i] = Arm.Neon.vsubq_s32(tileStartRemainder, tieBreaker);
                    v128 overflow = Arm.Neon.vshrq_n_s32(eventStartRemainder[i], 31);
                    eventStartRemainder[i] = Arm.Neon.vaddq_s32(eventStartRemainder[i], Arm.Neon.vandq_s8(overflow, edgeY[i]));
                    eventStartRemainder[i] = IntrinsicUtils._vblendq_f32(vy[i], eventStartRemainder[i], Arm.Neon.vsubq_s32(Arm.Neon.vsubq_s32(edgeY[i], eventStartRemainder[i]), one));

                    //eventStart[i] = xDiffi[i & 1] + tileStartSlope + (overflow & tileStartDir) + X86.Sse2.set1_epi32(FP_HALF_PIXEL - 1) + tieBreaker;
                    eventStart[i] = Arm.Neon.vaddq_s32(Arm.Neon.vaddq_s32(xDiffi[i & 1], tileStartSlope), Arm.Neon.vandq_s8(overflow, tileStartDir));
                    eventStart[i] = Arm.Neon.vaddq_s32(Arm.Neon.vaddq_s32(eventStart[i], new v128(BufferGroup.FpHalfPixel - 1)), tieBreaker);
                }

                // Split bounding box into bottom - middle - top region.
                v128 bbBottomIdx = Arm.Neon.vaddq_s32(bbTileMinX, Arm.Neon.vmulq_u32(bbTileMinY, new v128(NumTilesX)));
                v128 bbTopIdx = Arm.Neon.vaddq_s32(bbTileMinX, Arm.Neon.vmulq_u32(Arm.Neon.vaddq_s32(bbTileMinY, bbTileSizeY), new v128(NumTilesX)));
                v128 bbMidIdx = Arm.Neon.vaddq_s32(bbTileMinX, Arm.Neon.vmulq_u32(midTileY, new v128(NumTilesX)));

                // Loop over non-culled triangle and change SIMD axis to per-pixel
                while (triMask != 0)
                {
                    uint triIdx = (uint)IntrinsicUtils.find_clear_lsb(ref triMask);
                    int triMidVtxRight = (midVtxRight >> (int)triIdx) & 1;

                    // Get Triangle Zmin zMax
                    v128 zTriMax = new v128(IntrinsicUtils.getFloatLane(zMax, triIdx));
                    v128 zTriMin = new v128(IntrinsicUtils.getFloatLane(zMin, triIdx));

                    // Setup Zmin value for first set of 8x4 subtiles
                    v128 SimdSubTileColOffsetF = new v128(0f, BufferGroup.SubTileWidth, BufferGroup.SubTileWidth * 2, BufferGroup.SubTileWidth * 3);
                    v128 z0 = Arm.Neon.vmlaq_f32(Arm.Neon.vmlaq_f32(
                                                            new v128(IntrinsicUtils.getFloatLane(zPlaneOffset, triIdx)),
                                                            new v128(IntrinsicUtils.getFloatLane(zPixelDy, triIdx)),
                                                            zero),
                                                new v128(IntrinsicUtils.getFloatLane(zPixelDx, triIdx)),
                                                SimdSubTileColOffsetF);

                    float zx = IntrinsicUtils.getFloatLane(zTileDx, triIdx);
                    float zy = IntrinsicUtils.getFloatLane(zTileDy, triIdx);

                    // Get dimension of bounding box bottom, mid & top segments
                    int bbWidth = IntrinsicUtils.getIntLane(bbTileSizeX, triIdx);
                    int bbHeight = IntrinsicUtils.getIntLane(bbTileSizeY, triIdx);
                    int tileRowIdx = IntrinsicUtils.getIntLane(bbBottomIdx, triIdx);
                    int tileMidRowIdx = IntrinsicUtils.getIntLane(bbMidIdx, triIdx);
                    int tileEndRowIdx = IntrinsicUtils.getIntLane(bbTopIdx, triIdx);

                    if (bbWidth > BIG_TRIANGLE && bbHeight > BIG_TRIANGLE) // For big triangles we use a more expensive but tighter traversal algorithm
                    {
                        if (triMidVtxRight != 0)
                        {
                            RasterizeTriangleNEON(tiles, true, 1, triIdx, bbWidth, tileRowIdx, tileMidRowIdx, tileEndRowIdx, eventStart, slope, slopeTileDelta, zTriMin, zTriMax, ref z0, zx, zy, edgeY, absEdgeX, slopeSign, eventStartRemainder, slopeTileRemainder);
                        }
                        else
                        {
                            RasterizeTriangleNEON(tiles, true, 0, triIdx, bbWidth, tileRowIdx, tileMidRowIdx, tileEndRowIdx, eventStart, slope, slopeTileDelta, zTriMin, zTriMax, ref z0, zx, zy, edgeY, absEdgeX, slopeSign, eventStartRemainder, slopeTileRemainder);
                        }
                    }
                    else
                    {
                        if (triMidVtxRight != 0)
                        {
                            RasterizeTriangleNEON(tiles, false, 1, triIdx, bbWidth, tileRowIdx, tileMidRowIdx, tileEndRowIdx, eventStart, slope, slopeTileDelta, zTriMin, zTriMax, ref z0, zx, zy, edgeY, absEdgeX, slopeSign, eventStartRemainder, slopeTileRemainder);
                        }
                        else
                        {
                            RasterizeTriangleNEON(tiles, false, 0, triIdx, bbWidth, tileRowIdx, tileMidRowIdx, tileEndRowIdx, eventStart, slope, slopeTileDelta, zTriMin, zTriMax, ref z0, zx, zy, edgeY, absEdgeX, slopeSign, eventStartRemainder, slopeTileRemainder);
                        }
                    }
                }
            }
        }

        // RasterizeMesh now gets as input all triangles that already passed backface culling, clipping and an early z test
        // this should make it even easier for the system to keep using the full simd words and have better simd occupancy
        // instead of removing part of the work based on the mask
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void RasterizeMesh(Tile* tiles, float* binTriangleX, float* binTriangleY, float* binTriangleW, int numVert, ScissorRect screenScissor)
        {
            X86.MXCSRBits OldBits = X86.MXCSRBits.RoundToNearest;
            if (X86.Sse2.IsSse2Supported)
            {
                // Intel implementation needs a rounding workaround
                OldBits = X86.MXCSR;
                // DS: TODO: UNITY BURST FIX
                //using (var roundingMode = new X86.RoundingScope(X86.MXCSRBits.RoundToNearest))
                const X86.MXCSRBits roundingMode = X86.MXCSRBits.RoundToNearest;
                X86.MXCSR = (OldBits & ~X86.MXCSRBits.RoundingControlMask) | roundingMode;
            }

            int vertexIndex = 0;

            v128* vtxX_prealloc = stackalloc v128[3];
            v128* vtxY_prealloc = stackalloc v128[3];
            v128* vtxW_prealloc = stackalloc v128[3];

            v128* pVtxX_prealloc = stackalloc v128[3];
            v128* pVtxY_prealloc = stackalloc v128[3];
            v128* pVtxZ_prealloc = stackalloc v128[3];

            v128* ipVtxX_prealloc = stackalloc v128[3];
            v128* ipVtxY_prealloc = stackalloc v128[3];

            while (vertexIndex < numVert)
            {
                v128* vtxX = vtxX_prealloc;
                v128* vtxY = vtxY_prealloc;
                v128* vtxW = vtxW_prealloc;

                int numLanes = math.min(SIMD_LANES, numVert - vertexIndex);
                uint triMask = (1u << numLanes) - 1;

                for (int i = 0; i < 3; i++)
                {
                    if (X86.Sse2.IsSse2Supported)
                    {
                        vtxX[i] = X86.Sse.load_ps(&binTriangleX[vertexIndex + i * 4]);
                        vtxY[i] = X86.Sse.load_ps(&binTriangleY[vertexIndex + i * 4]);
                        vtxW[i] = X86.Sse.load_ps(&binTriangleW[vertexIndex + i * 4]);
                    }
                    else if (Arm.Neon.IsNeonSupported)
                    {
                        vtxX[i] = Arm.Neon.vld1q_f32(&binTriangleX[vertexIndex + i * 4]);
                        vtxY[i] = Arm.Neon.vld1q_f32(&binTriangleY[vertexIndex + i * 4]);
                        vtxW[i] = Arm.Neon.vld1q_f32(&binTriangleW[vertexIndex + i * 4]);
                    }
                }

                vertexIndex += SIMD_LANES * 3;

                if (triMask == 0x0)
                {
                    continue;
                }

                /* Project and transform to screen space. Note
                    that we use z = 1.0 / vtx.w for depth, which means that z = 0 is far and
                    z = 1/m_near is near. For ortho projection, we do z = (z * -1) + 1 to go from z = 0 for far and z = 2 for near

                    We must also use a greater than depth test, and in effect
                    everything is reversed compared to regular z implementations. */

                v128* pVtxX = pVtxX_prealloc;
                v128* pVtxY = pVtxY_prealloc;
                v128* pVtxZ = pVtxZ_prealloc;

                v128* ipVtxX = ipVtxX_prealloc;
                v128* ipVtxY = ipVtxY_prealloc;
                if (X86.Sse2.IsSse2Supported)
                {
                    ProjectVerticesSSE(ipVtxX, ipVtxY, pVtxX, pVtxY, pVtxZ, vtxX, vtxY, vtxW);
                }
                else if (Arm.Neon.IsNeonSupported)
                {
                    ProjectVerticesNEON(ipVtxX, ipVtxY, pVtxX, pVtxY, pVtxZ, vtxX, vtxY, vtxW);
                }

                /* Setup and rasterize a SIMD batch of triangles */
                if (X86.Sse2.IsSse2Supported)
                {
                    RasterizeTriangleBatchSSE(tiles, ipVtxX, ipVtxY, pVtxX, pVtxY, pVtxZ, triMask, screenScissor);
                    // Don't forget to restore the rounding mode
                    // DS: TODO: UNITY BURST FIX
                    X86.MXCSR = OldBits;
                }
                else if (Arm.Neon.IsNeonSupported)
                {
                    RasterizeTriangleBatchNEON(tiles, ipVtxX, ipVtxY, pVtxX, pVtxY, pVtxZ, triMask, screenScissor);
                }
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        void RasterizeTriangleNEON(
            Tile* tiles,
            bool isTightTraversal,
            int midVtxRight,
            uint triIdx,
            int bbWidth,
            int tileRowIdx,
            int tileMidRowIdx,
            int tileEndRowIdx,
            v128* eventStart,
            v128* slope,
            v128* slopeTileDelta,
            v128 zTriMin,
            v128 zTriMax,
            ref v128 z0,
            float zx,
            float zy,
            v128* edgeY,
            v128* absEdgeX,
            v128* slopeSign,
            v128* eventStartRemainder,
            v128* slopeTileRemainder)
        {
            if (Arm.Neon.IsNeonSupported)
            {
                const int LEFT_EDGE_BIAS = -1;
                const int RIGHT_EDGE_BIAS = 1;

                v128* triSlopeSign = stackalloc v128[3];
                v128* triSlopeTileDelta = stackalloc v128[3];
                v128* triEdgeY = stackalloc v128[3];
                v128* triSlopeTileRemainder = stackalloc v128[3];
                v128* triEventRemainder = stackalloc v128[3];
                v128* triEvent = stackalloc v128[3];

                for (int i = 0; i < 3; ++i)
                {
                    triSlopeSign[i] = new v128(IntrinsicUtils.getIntLane(slopeSign[i], triIdx));
                    triSlopeTileDelta[i] = new v128(IntrinsicUtils.getIntLane(slopeTileDelta[i], triIdx));
                    triEdgeY[i] = new v128(IntrinsicUtils.getIntLane(edgeY[i], triIdx));
                    triSlopeTileRemainder[i] = new v128(IntrinsicUtils.getIntLane(slopeTileRemainder[i], triIdx));

                    v128 triSlope = new v128(IntrinsicUtils.getFloatLane(slope[i], triIdx));
                    v128 triAbsEdgeX = new v128(IntrinsicUtils.getIntLane(absEdgeX[i], triIdx));
                    v128 triStartRemainder = new v128(IntrinsicUtils.getIntLane(eventStartRemainder[i], triIdx));
                    v128 triEventStart = new v128(IntrinsicUtils.getIntLane(eventStart[i], triIdx));

                    v128 SimdLaneYCoordF = new v128(128f, 384f, 640f, 896f);
                    v128 scanlineDelta = Arm.Neon.vcvtnq_s32_f32(Arm.Neon.vmulq_f32(triSlope, SimdLaneYCoordF));
                    v128 SimdLaneYCoordI = new v128(128, 384, 640, 896);
                    v128 scanlineSlopeRemainder = Arm.Neon.vsubq_s32(Arm.Neon.vmulq_u32(triAbsEdgeX, SimdLaneYCoordI), Arm.Neon.vmulq_u32(Arm.Neon.vabsq_s32(scanlineDelta), triEdgeY[i]));

                    triEventRemainder[i] = Arm.Neon.vsubq_s32(triStartRemainder, scanlineSlopeRemainder);
                    v128 overflow = Arm.Neon.vshrq_n_s32(triEventRemainder[i], 31);
                    triEventRemainder[i] = Arm.Neon.vaddq_s32(triEventRemainder[i], Arm.Neon.vandq_s8(overflow, triEdgeY[i]));
                    triEvent[i] = Arm.Neon.vaddq_s32(Arm.Neon.vaddq_s32(triEventStart, scanlineDelta), Arm.Neon.vandq_s8(overflow, triSlopeSign[i]));
                }

                // For big triangles track start & end tile for each scanline and only traverse the valid region
                int startDelta = 0;
                int endDelta = 0;
                int topDelta = 0;
                int startEvent = 0;
                int endEvent = 0;
                int topEvent = 0;

                if (isTightTraversal)
                {
                    startDelta = IntrinsicUtils.getIntLane(slopeTileDelta[2], triIdx) + LEFT_EDGE_BIAS;
                    endDelta = IntrinsicUtils.getIntLane(slopeTileDelta[0], triIdx) + RIGHT_EDGE_BIAS;
                    topDelta = IntrinsicUtils.getIntLane(slopeTileDelta[1], triIdx) +
                                (midVtxRight != 0 ? RIGHT_EDGE_BIAS : LEFT_EDGE_BIAS);

                    // Compute conservative bounds for the edge events over a 32xN tile
                    startEvent = IntrinsicUtils.getIntLane(eventStart[2], triIdx) + Mathf.Min(0, startDelta);
                    endEvent = IntrinsicUtils.getIntLane(eventStart[0], triIdx) + Mathf.Max(0, endDelta) +
                                (BufferGroup.TileWidth << BufferGroup.FpBits); // TODO: (Apoorva) can be spun out into a const

                    if (midVtxRight != 0)
                    {
                        topEvent = IntrinsicUtils.getIntLane(eventStart[1], triIdx) + Mathf.Max(0, topDelta) +
                                    (BufferGroup.TileWidth << BufferGroup.FpBits); // TODO: (Apoorva) can be spun out into a const
                    }
                    else
                    {
                        topEvent = IntrinsicUtils.getIntLane(eventStart[1], triIdx) + Mathf.Min(0, topDelta);
                    }
                }

                if (tileRowIdx <= tileMidRowIdx)
                {
                    int tileStopIdx = Mathf.Min(tileEndRowIdx, tileMidRowIdx);

                    // Traverse the bottom half of the triangle
                    while (tileRowIdx < tileStopIdx)
                    {
                        int start = 0;
                        int end = bbWidth;

                        if (isTightTraversal)
                        {
                            // Compute tighter start and endpoints to avoid traversing empty space
                            start = Mathf.Max(0, Mathf.Min(bbWidth - 1, startEvent >> (BufferGroup.TileWidthShift + BufferGroup.FpBits)));
                            end = Mathf.Min(bbWidth, (endEvent >> (BufferGroup.TileWidthShift + BufferGroup.FpBits)));

                            startEvent += startDelta;
                            endEvent += endDelta;
                        }

                        // Traverse the scanline and update the masked hierarchical z buffer
                        TraverseScanlineNEON(tiles, 1, 1, start, end, tileRowIdx, 0, 2, triEvent, zTriMin, zTriMax, z0, zx);

                        // move to the next scanline of tiles, update edge events and interpolate z
                        tileRowIdx += NumTilesX;
                        z0 = Arm.Neon.vaddq_f32(z0, new v128(zy));

                        UpdateTileEventsYNEON(triEventRemainder, triSlopeTileRemainder, triEdgeY, triEvent,
                            triSlopeTileDelta, triSlopeSign, 0);
                        UpdateTileEventsYNEON(triEventRemainder, triSlopeTileRemainder, triEdgeY, triEvent,
                            triSlopeTileDelta, triSlopeSign, 2);
                    }

                    // Traverse the middle scanline of tiles. We must consider all three edges only in this region
                    if (tileRowIdx < tileEndRowIdx)
                    {
                        int start = 0;
                        int end = bbWidth;

                        if (isTightTraversal)
                        {
                            // Compute tighter start and endpoints to avoid traversing lots of empty space
                            start = Mathf.Max(0, Mathf.Min(bbWidth - 1, startEvent >> (BufferGroup.TileWidthShift + BufferGroup.FpBits)));
                            end = Mathf.Min(bbWidth, (endEvent >> (BufferGroup.TileWidthShift + BufferGroup.FpBits)));

                            // Switch the traversal start / end to account for the upper side edge
                            endEvent = midVtxRight != 0 ? topEvent : endEvent;
                            endDelta = midVtxRight != 0 ? topDelta : endDelta;
                            startEvent = midVtxRight != 0 ? startEvent : topEvent;
                            startDelta = midVtxRight != 0 ? startDelta : topDelta;

                            startEvent += startDelta;
                            endEvent += endDelta;
                        }

                        // Traverse the scanline and update the masked hierarchical z buffer.
                        if (midVtxRight != 0)
                        {
                            TraverseScanlineNEON(tiles, 2, 1, start, end, tileRowIdx, 0, 2, triEvent, zTriMin, zTriMax,
                                z0, zx);
                        }
                        else
                        {
                            TraverseScanlineNEON(tiles, 1, 2, start, end, tileRowIdx, 0, 2, triEvent, zTriMin, zTriMax,
                                z0, zx);
                        }

                        tileRowIdx += NumTilesX;
                    }

                    // Traverse the top half of the triangle
                    if (tileRowIdx < tileEndRowIdx)
                    {
                        // move to the next scanline of tiles, update edge events and interpolate z
                        z0 = Arm.Neon.vaddq_f32(z0, new v128(zy));
                        int i0 = midVtxRight + 0;
                        int i1 = midVtxRight + 1;

                        UpdateTileEventsYNEON(triEventRemainder, triSlopeTileRemainder, triEdgeY, triEvent,
                            triSlopeTileDelta, triSlopeSign, i0);
                        UpdateTileEventsYNEON(triEventRemainder, triSlopeTileRemainder, triEdgeY, triEvent,
                            triSlopeTileDelta, triSlopeSign, i1);

                        for (; ; )
                        {
                            int start = 0;
                            int end = bbWidth;

                            if (isTightTraversal)
                            {
                                // Compute tighter start and endpoints to avoid traversing lots of empty space
                                start = Mathf.Max(0, Mathf.Min(bbWidth - 1, startEvent >> (BufferGroup.TileWidthShift + BufferGroup.FpBits)));
                                end = Mathf.Min(bbWidth, (endEvent >> (BufferGroup.TileWidthShift + BufferGroup.FpBits)));

                                startEvent += startDelta;
                                endEvent += endDelta;
                            }

                            // Traverse the scanline and update the masked hierarchical z buffer
                            TraverseScanlineNEON(tiles, 1, 1, start, end, tileRowIdx, midVtxRight + 0,
                                midVtxRight + 1, triEvent, zTriMin, zTriMax, z0, zx);

                            // move to the next scanline of tiles, update edge events and interpolate z
                            tileRowIdx += NumTilesX;
                            if (tileRowIdx >= tileEndRowIdx)
                            {
                                break;
                            }

                            z0 = Arm.Neon.vaddq_f32(z0, new v128(zy));

                            UpdateTileEventsYNEON(triEventRemainder, triSlopeTileRemainder, triEdgeY, triEvent,
                                triSlopeTileDelta, triSlopeSign, i0);
                            UpdateTileEventsYNEON(triEventRemainder, triSlopeTileRemainder, triEdgeY, triEvent,
                                triSlopeTileDelta, triSlopeSign, i1);
                        }
                    }
                }
                else
                {
                    if (isTightTraversal)
                    {
                        // For large triangles, switch the traversal start / end to account for the upper side edge
                        endEvent = midVtxRight != 0 ? topEvent : endEvent;
                        endDelta = midVtxRight != 0 ? topDelta : endDelta;
                        startEvent = midVtxRight != 0 ? startEvent : topEvent;
                        startDelta = midVtxRight != 0 ? startDelta : topDelta;
                    }

                    // Traverse the top half of the triangle
                    if (tileRowIdx < tileEndRowIdx)
                    {
                        int i0 = midVtxRight + 0;
                        int i1 = midVtxRight + 1;

                        for (; ; )
                        {
                            int start = 0;
                            int end = bbWidth;

                            if (isTightTraversal)
                            {
                                // Compute tighter start and endpoints to avoid traversing lots of empty space
                                start = Mathf.Max(0, Mathf.Min(bbWidth - 1, startEvent >> (BufferGroup.TileWidthShift + BufferGroup.FpBits)));
                                end = Mathf.Min(bbWidth, (endEvent >> (BufferGroup.TileWidthShift + BufferGroup.FpBits)));

                                startEvent += startDelta;
                                endEvent += endDelta;
                            }

                            // Traverse the scanline and update the masked hierarchical z buffer
                            TraverseScanlineNEON(tiles, 1, 1, start, end, tileRowIdx, midVtxRight + 0,
                                midVtxRight + 1, triEvent, zTriMin, zTriMax, z0, zx);

                            // move to the next scanline of tiles, update edge events and interpolate z
                            tileRowIdx += NumTilesX;
                            if (tileRowIdx >= tileEndRowIdx)
                            {
                                break;
                            }

                            z0 = Arm.Neon.vaddq_f32(z0, new v128(zy));

                            UpdateTileEventsYNEON(triEventRemainder, triSlopeTileRemainder, triEdgeY, triEvent,
                                triSlopeTileDelta, triSlopeSign, i0);
                            UpdateTileEventsYNEON(triEventRemainder, triSlopeTileRemainder, triEdgeY, triEvent,
                                triSlopeTileDelta, triSlopeSign, i1);
                        }
                    }
                }
            }
        }
        #endregion

        public void Execute(int i)
        {
            var tiles = &TilesBasePtr[0];
            ScissorRect scissorRect = new ScissorRect();

            int2 pixelsPerTile = new int2(NumPixelsX / NumTilesX, NumPixelsY / NumTilesY);

            float* temp_stack_x = stackalloc float[12];
            float* temp_stack_y = stackalloc float[12];
            float* temp_stack_w = stackalloc float[12];
            int tempStackSize = 0;

            int countOfTilesX = NumTilesX / TilesPerBinX;
            scissorRect.mMinX = (i % countOfTilesX) * pixelsPerTile.x * TilesPerBinX;
            scissorRect.mMaxX = scissorRect.mMinX + pixelsPerTile.x * TilesPerBinX;
            scissorRect.mMinY = (i / countOfTilesX) * pixelsPerTile.y * TilesPerBinY;
            scissorRect.mMaxY = scissorRect.mMinY + pixelsPerTile.y * TilesPerBinY;

            float4 clipRect = new float4(scissorRect.mMinX, scissorRect.mMinY, scissorRect.mMaxX, scissorRect.mMaxY);
            clipRect = (2 * clipRect.xyzw / (new float2(NumPixelsX, NumPixelsY).xyxy) - 1);

            int bufferIndex = ( WorkerIndex - 1 );
            float* binTriangleX = (float*)BinTriangleXBasePtr + BinSize * bufferIndex;
            float* binTriangleY = (float*)BinTriangleYBasePtr + BinSize * bufferIndex;
            float* binTriangleW = (float*)BinTriangleWBasePtr + BinSize * bufferIndex;

            // For each mesh
            // if the mesh aabb is inside the bin aabb
            // check all each triangle and test against the bin aabb
            // if inside the bin, add in, once the bin is full render it
            // once the loop finish, render the remaining triangles in the bin
            int internalBinSize = 0;
            for (int m = 0; m < ClippedOccluders.Length; m += 1)
            {
                float2 max = ClippedOccluders[m].screenMax.xy;
                float2 min = ClippedOccluders[m].screenMin.xy;

                if (math.any(min > clipRect.zw) || math.any(max < clipRect.xy))
                    continue;

                ClippedOccluder clipped = ClippedOccluders[m];

                int k = 0;
                for (int j = 0; j < clipped.expandedVertexSize; j += 3, ++k)
                {
                    float4 triExtents = ClippedTriExtents[clipped.sourceIndexOffset * 2 + k];
                    min = triExtents.xy;
                    max = triExtents.zw;

                    if (math.any(min > clipRect.zw) || math.any(max < clipRect.xy))
                        continue;

                    for (int n = 0; n < 3; ++n)
                    {
                        float3 vert = ClippedVerts[clipped.sourceIndexOffset * 6 + j + n];
                        temp_stack_x[tempStackSize] = vert.x;
                        temp_stack_y[tempStackSize] = vert.y;
                        temp_stack_w[tempStackSize] = vert.z;
                        tempStackSize++;
                    }

                    if (tempStackSize == 12)
                    {
                        for (int n = 0; n < 3; ++n)
                        {
                            for (int p = 0; p < 4; ++p)
                            {
                                binTriangleX[internalBinSize + p + n * 4] = temp_stack_x[n + p * 3];
                                binTriangleY[internalBinSize + p + n * 4] = temp_stack_y[n + p * 3];
                                binTriangleW[internalBinSize + p + n * 4] = temp_stack_w[n + p * 3];
                            }
                        }
                        internalBinSize += 12;
                        tempStackSize = 0;
                    }
                    if (internalBinSize == BinSize)
                    {
                        RasterizeMesh(tiles, binTriangleX, binTriangleY, binTriangleW, internalBinSize, scissorRect);
                        internalBinSize = 0;
                    }
                }
            }
            if (tempStackSize > 0)
            {
                for (int n = 0; n < 3; ++n)
                {
                    for (int p = 0; p < 4; ++p)
                    {
                        binTriangleX[internalBinSize + p + n * 4] = temp_stack_x[n + p * 3];
                        binTriangleY[internalBinSize + p + n * 4] = temp_stack_y[n + p * 3];
                        binTriangleW[internalBinSize + p + n * 4] = temp_stack_w[n + p * 3];
                    }
                }
                internalBinSize += tempStackSize;
                tempStackSize = 0;
            }
            if (internalBinSize > 0)
            {
                RasterizeMesh(tiles, binTriangleX, binTriangleY, binTriangleW, internalBinSize, scissorRect);
            }
        }
    }
}

#endif // ENABLE_UNITY_OCCLUSION && (HDRP_10_0_0_OR_NEWER || URP_10_0_0_OR_NEWER)
