using UnityEngine;
using Unity.Collections;
using Unity.Jobs;
using Unity.Burst;
using Pathfinding.Util;
using Pathfinding.Collections;

namespace Pathfinding.Graphs.Navmesh.Voxelization.Burst {
	/// <summary>VoxelContour used for recast graphs.</summary>
	public struct VoxelContour {
		public int nverts;

		/// <summary>Vertex coordinates, each vertex contains 4 components.</summary>
		public int vertexStartIndex;

		/// <summary>Region ID of the contour</summary>
		public int reg;

		/// <summary>Area ID of the contour.</summary>
		public int area;
	}

	[BurstCompile(CompileSynchronously = true)]
	public struct JobBuildContours : IJob {
		public CompactVoxelField field;
		public float maxError;
		public float maxEdgeLength;
		public int buildFlags;
		public float cellSize;
		public NativeList<VoxelContour> outputContours;
		public NativeList<int> outputVerts;

		public void Execute () {
        }

        void GetClosestIndices(NativeArray<int> verts, int vertexStartIndexA, int nvertsa,
                                int vertexStartIndexB, int nvertsb,
                                out int ia, out int ib)
        {
            ia = default(int);
            ib = default(int);
        }

        public static bool MergeContours(NativeList<int> verts, ref VoxelContour ca, ref VoxelContour cb, int ia, int ib)
        {
            return default;
        }

        public void SimplifyContour(NativeList<int> verts, NativeList<int> simplified, float maxError, int buildFlags)
        {
        }

        public void WalkContour(int x, int z, int i, NativeArray<ushort> flags, NativeList<int> verts)
        {
        }

        public int GetCornerHeight(int x, int z, int i, int dir, ref bool isBorderVertex)
        {
            return default;
        }

        static void RemoveRange(NativeList<int> arr, int index, int count)
        {
        }

        static void RemoveDegenerateSegments(NativeList<int> simplified)
        {
        }

        int CalcAreaOfPolygon2D(NativeArray<int> verts, int vertexStartIndex, int nverts)
        {
            return default;
        }

        static bool Ileft (NativeArray<int> verts, int a, int b, int c) {
            return default;
        }
    }
}
