using UnityEngine;
using Unity.Collections;
using Unity.Mathematics;
using Unity.Jobs;
using Unity.Burst;
using Pathfinding.Util;

namespace Pathfinding.Graphs.Navmesh.Voxelization.Burst {
	[BurstCompile(CompileSynchronously = true)]
	public struct JobBuildRegions : IJob {
		public CompactVoxelField field;
		public NativeList<ushort> distanceField;
		public int borderSize;
		public int minRegionSize;
		public NativeQueue<Int3> srcQue;
		public NativeQueue<Int3> dstQue;
		public RecastGraph.RelevantGraphSurfaceMode relevantGraphSurfaceMode;
		public NativeArray<RelevantGraphSurfaceInfo> relevantGraphSurfaces;

		public float cellSize, cellHeight;
		public Matrix4x4 graphTransform;
		public Bounds graphSpaceBounds;

		void MarkRectWithRegion (int minx, int maxx, int minz, int maxz, ushort region, NativeArray<ushort> srcReg) {
        }

        public static bool FloodRegion(int x, int z, int i, uint level, ushort r,
                                        CompactVoxelField field,
                                        NativeArray<ushort> distanceField,
                                        NativeArray<ushort> srcReg,
                                        NativeArray<ushort> srcDist,
                                        NativeArray<Int3> stack,
                                        NativeArray<int> flags,
                                        NativeArray<bool> closed)
        {
            return default;
        }

        public void Execute()
        {
        }

        /// <summary>
        /// Find method in the UnionFind data structure.
        /// See: https://en.wikipedia.org/wiki/Disjoint-set_data_structure
        /// </summary>
        static int union_find_find(NativeArray<int> arr, int x)
        {
            return default;
        }

        /// <summary>
        /// Join method in the UnionFind data structure.
        /// See: https://en.wikipedia.org/wiki/Disjoint-set_data_structure
        /// </summary>
        static void union_find_union(NativeArray<int> arr, int a, int b)
        {
        }

        public struct RelevantGraphSurfaceInfo {
			public float3 position;
			public float range;
		}

		/// <summary>Filters out or merges small regions.</summary>
		public static void FilterSmallRegions (CompactVoxelField field, NativeArray<ushort> reg, int minRegionSize, int maxRegions, NativeArray<RelevantGraphSurfaceInfo> relevantGraphSurfaces, RecastGraph.RelevantGraphSurfaceMode relevantGraphSurfaceMode, float4x4 voxel2worldMatrix)
        {
        }
    }

    static class VoxelUtilityBurst
    {
        /// <summary>All bits in the region which will be interpreted as a tag.</summary>
        public const int TagRegMask = TagReg - 1;

        /// <summary>
        /// If a cell region has this bit set then
        /// The remaining region bits (see <see cref="TagRegMask)"/> will be used for the node's tag.
        /// </summary>
        public const int TagReg = 1 << 14;

        /// <summary>
        /// If heightfield region ID has the following bit set, the region is on border area
        /// and excluded from many calculations.
        /// </summary>
        public const ushort BorderReg = 1 << 15;

        /// <summary>
        /// If contour region ID has the following bit set, the vertex will be later
        /// removed in order to match the segments and vertices at tile boundaries.
        /// </summary>
        public const int RC_BORDER_VERTEX = 1 << 16;

        public const int RC_AREA_BORDER = 1 << 17;

        public const int VERTEX_BUCKET_COUNT = 1 << 12;

        /// <summary>Tessellate wall edges</summary>
        public const int RC_CONTOUR_TESS_WALL_EDGES = 1 << 0;

        /// <summary>Tessellate edges between areas</summary>
        public const int RC_CONTOUR_TESS_AREA_EDGES = 1 << 1;

        /// <summary>Tessellate edges at the border of the tile</summary>
        public const int RC_CONTOUR_TESS_TILE_EDGES = 1 << 2;

        /// <summary>Mask used with contours to extract region id.</summary>
        public const int ContourRegMask = 0xffff;

        public static readonly int[] DX = new int[] { -1, 0, 1, 0 };
        public static readonly int[] DZ = new int[] { 0, 1, 0, -1 };

        public static void CalculateDistanceField(CompactVoxelField field, NativeArray<ushort> output)
        {
        }

        public static void BoxBlur(CompactVoxelField field, NativeArray<ushort> src, NativeArray<ushort> dst)
        {
        }
    }
}
