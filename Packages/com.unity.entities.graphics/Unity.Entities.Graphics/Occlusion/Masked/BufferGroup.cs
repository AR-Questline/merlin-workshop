#if ENABLE_UNITY_OCCLUSION && (HDRP_10_0_0_OR_NEWER || URP_10_0_0_OR_NEWER)

using Unity.Burst.Intrinsics;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Rendering.Occlusion.Masked.Visualization;
using UnityEngine;
using UnityEngine.Profiling;
using UnityEngine.Rendering;

namespace Unity.Rendering.Occlusion.Masked
{

    class BufferGroup
    {
        public const int TileWidthShift = 5;
        public const int TileHeightShift = 2;
        public const int TileWidth = 1 << TileWidthShift;
        public const int TileHeight = 1 << TileHeightShift;
        // Sub-tiles (used for updating the masked HiZ buffer) are 8x4 tiles, so there are 4x2 sub-tiles in a tile
        public const int SubTileWidth = 8;
        public const int SubTileHeight = 4;
        // The number of fixed point bits used to represent vertex coordinates / edge slopes.
        public const int FpBits = 8;
        public const int FpHalfPixel = 1 << (FpBits - 1);
        // Tile dimensions in fixed point coordinates
        public const int FpTileHeightShift = (FpBits + TileHeightShift);
        public const int FpTileHeight = (1 << FpTileHeightShift);
        // Size of guard band in pixels. Clipping doesn't seem to be very expensive so we use a small guard band
        // to improve rasterization performance. It's not recommended to set the guard band to zero, as this may
        // cause leakage along the screen border due to precision/rounding.
        public const float GuardBandPixelSize = 1f;

        // Depth buffer
        public int NumBuffers;
        public int NumPixelsX;
        public int NumPixelsY;
        public int NumTilesX;
        public int NumTilesY;

        public NativeArray<Tile> Tiles;

        // Resolution-dependent values
        public v128 PixelCenterX;
        public v128 PixelCenterY;
        public v128 PixelCenter;
        public v128 HalfWidth;
        public v128 HalfHeight;
        public v128 HalfSize;
        public v128 ScreenSize;

        public readonly BatchCullingViewType ViewType;
        public Matrix4x4 CullingMatrix;
        public BatchCullingProjectionType ProjectionType;
        public float NearClip;
        public NativeArray<v128> FrustumPlanes;
        public ScissorRect FullScreenScissor;

        // Visualization
        DebugView m_DebugView;

        public bool Enabled;

        public BufferGroup(BatchCullingViewType viewType)
        {
            ViewType = viewType;
            NumBuffers = math.clamp(Jobs.LowLevel.Unsafe.JobsUtility.JobWorkerMaximumCount, 1, 10);
            NearClip = float.MaxValue;
            FrustumPlanes = new NativeArray<v128>(5, Allocator.Persistent);
            Enabled = true;
        }

        public void Dispose()
        {
            FrustumPlanes.Dispose();
            if (Tiles.IsCreated)
            {
                Tiles.Dispose();
            }

            m_DebugView?.Dispose();
        }

        public void SetResolutionAndClip(int numPixelsX, int numPixelsY, BatchCullingProjectionType projectionType, float nearClip)
        {
            if (numPixelsX != NumPixelsX || numPixelsY != NumPixelsY || projectionType != ProjectionType)
            {
                NumPixelsX = numPixelsX;
                NumPixelsY = numPixelsY;
                NumTilesX = (numPixelsX + TileWidth - 1) >> TileWidthShift;
                NumTilesY = (numPixelsY + TileHeight - 1) >> TileHeightShift;

                float w = numPixelsX; // int -> float
                float h = numPixelsY; //
                float hw = w * 0.5f;
                float hh = h * 0.5f;
                PixelCenterX = new v128(hw);
                PixelCenterY = new v128(hh);
                PixelCenter = new v128(hw, hw, hh, hh);
                HalfWidth = new v128(hw);
                HalfHeight = new v128(-hh);
                HalfSize = new v128(hw, hw, -hh, -hh);
                ScreenSize = new v128(numPixelsX - 1, numPixelsX - 1, numPixelsY - 1, numPixelsY - 1);
                // TODO: Delete this after full implementation. This isn't needed because min values are zero, and
                // so there is opportunity for optimization.
                // Setup a full screen scissor rectangle
                FullScreenScissor.mMinX = 0;
                FullScreenScissor.mMinY = 0;
                FullScreenScissor.mMaxX = NumTilesX << TileWidthShift;
                FullScreenScissor.mMaxY = NumTilesY << TileHeightShift;
                // Allocate the tiles buffers
                if (Tiles.IsCreated)
                {
                    Tiles.Dispose();
                }

                Tiles = new NativeArray<Tile>(NumBuffers * NumTilesX * NumTilesY,
                    Allocator.Persistent, NativeArrayOptions.UninitializedMemory);

                // Set orthographic mode and the rest of the frustum planes
                {
                    ProjectionType = projectionType;
                    float guardBandWidth = 2f / NumPixelsX;
                    float guardBandHeight = 2f / NumPixelsY;

                    if (projectionType == BatchCullingProjectionType.Orthographic)
                    {
                        FrustumPlanes[1] = new v128(1f - guardBandWidth, 0f, 0f, 1f);
                        FrustumPlanes[2] = new v128(-1f + guardBandWidth, 0f, 0f, 1f);
                        FrustumPlanes[3] = new v128(0f, 1f - guardBandHeight, 0f, 1f);
                        FrustumPlanes[4] = new v128(0f, -1f + guardBandHeight, 0f, 1f);
                    }
                    else
                    {
                        FrustumPlanes[1] = new v128(1f - guardBandWidth, 0f, 1f, 0f);
                        FrustumPlanes[2] = new v128(-1f + guardBandWidth, 0f, 1f, 0f);
                        FrustumPlanes[3] = new v128(0f, 1f - guardBandHeight, 1f, 0f);
                        FrustumPlanes[4] = new v128(0f, -1f + guardBandHeight, 1f, 0f);
                    }
                }
            }

            if (NearClip != nearClip)
            {
                // Set near clip
                NearClip = nearClip;
                FrustumPlanes[0] = new v128(0f, 0f, 1f, -nearClip);
            }
        }

        public Texture2D GetVisualizationTexture()
        {
            if (m_DebugView != null)
            {
                m_DebugView.ReallocateIfNeeded(NumPixelsX, NumPixelsY);
                return m_DebugView.gpuDepth;
            }
            return null;
        }

        public void RenderToTextures(EntityQuery testQuery, EntityQuery meshQuery, JobHandle dependency, DebugRenderMode mode)
        {
            if (mode == DebugRenderMode.None
#if UNITY_EDITOR
                && !OcclusionBrowseWindow.IsVisible
#endif
                )
            {
                return;
            }

            bool refresh = (m_DebugView == null);
            if (refresh)
            {
                m_DebugView = new DebugView();
            }

            dependency.Complete();

            m_DebugView.ReallocateIfNeeded(NumPixelsX, NumPixelsY);

            Profiler.BeginSample("Occlusion.Debug.RenderView");
            m_DebugView.RenderToTextures(testQuery, meshQuery, this, mode
#if UNITY_EDITOR
                , OcclusionBrowseWindow.IsVisible
#endif
                );
            Profiler.EndSample();

#if UNITY_EDITOR
            if (refresh)
            {
                OcclusionBrowseWindow.Refresh();
            }
#endif
        }

        public void RenderToCamera(DebugRenderMode renderMode, Camera camera, CommandBuffer cmd, Mesh fullScreenQuad)
        {
            m_DebugView?.RenderToCamera(renderMode, camera, cmd, fullScreenQuad, CullingMatrix);
        }
    }
}

#endif // ENABLE_UNITY_OCCLUSION && (HDRP_10_0_0_OR_NEWER || URP_10_0_0_OR_NEWER)
