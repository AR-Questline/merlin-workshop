using UnityEngine;
using Unity.Collections;
using Unity.Jobs;
using Unity.Burst;

namespace Pathfinding.Graphs.Navmesh.Voxelization.Burst {
	using System;
	using Pathfinding.Jobs;
	using Pathfinding.Collections;
#if MODULE_COLLECTIONS_2_1_0_OR_NEWER
	using NativeHashMapInt3Int = Unity.Collections.NativeHashMap<Int3, int>;
#else
	using NativeHashMapInt3Int = Unity.Collections.NativeParallelHashMap<Int3, int>;
#endif

	/// <summary>VoxelMesh used for recast graphs.</summary>
	public struct VoxelMesh : IArenaDisposable {
		/// <summary>Vertices of the mesh</summary>
		public NativeList<Int3> verts;

		/// <summary>
		/// Triangles of the mesh.
		/// Each element points to a vertex in the <see cref="verts"/> array
		/// </summary>
		public NativeList<int> tris;

		/// <summary>Area index for each triangle</summary>
		public NativeList<int> areas;

		void IArenaDisposable.DisposeWith (DisposeArena arena) {
        }
    }

	/// <summary>Builds a polygon mesh from a contour set.</summary>
	[BurstCompile]
	public struct JobBuildMesh : IJob {
		public NativeList<int> contourVertices;
		/// <summary>contour set to build a mesh from.</summary>
		public NativeList<VoxelContour> contours;
		/// <summary>Results will be written to this mesh.</summary>
		public VoxelMesh mesh;
		public CompactVoxelField field;

		/// <summary>
		/// Returns T iff (v_i, v_j) is a proper internal
		/// diagonal of P.
		/// </summary>
		static bool Diagonal (int i, int j, int n, NativeArray<int> verts, NativeArray<int> indices) {
            return default;
        }

        static bool InCone (int i, int j, int n, NativeArray<int> verts, NativeArray<int> indices) {
            return default;
        }

        /// <summary>
        /// Returns true iff c is strictly to the left of the directed
        /// line through a to b.
        /// </summary>
        static bool Left (int a, int b, int c, NativeArray<int> verts) {
            return default;
        }

        static bool LeftOn (int a, int b, int c, NativeArray<int> verts) {
            return default;
        }

        static bool Collinear (int a, int b, int c, NativeArray<int> verts) {
            return default;
        }

        public static int Area2 (int a, int b, int c, NativeArray<int> verts) {
            return default;
        }

        /// <summary>
        /// Returns T iff (v_i, v_j) is a proper internal *or* external
        /// diagonal of P, *ignoring edges incident to v_i and v_j*.
        /// </summary>
        static bool Diagonalie (int i, int j, int n, NativeArray<int> verts, NativeArray<int> indices) {
            return default;
        }

        //	Exclusive or: true iff exactly one argument is true.
        //	The arguments are negated to ensure that they are 0/1
        //	values.  Then the bitwise Xor operator may apply.
        //	(This idea is due to Michael Baldwin.)
        static bool Xorb (bool x, bool y) {
            return default;
        }

        //	Returns true iff ab properly intersects cd: they share
        //	a point interior to both segments.  The properness of the
        //	intersection is ensured by using strict leftness.
        static bool IntersectProp (int a, int b, int c, int d, NativeArray<int> verts) {
            return default;
        }

        // Returns T iff (a,b,c) are collinear and point c lies
        // on the closed segement ab.
        static bool Between (int a, int b, int c, NativeArray<int> verts) {
            return default;
        }

        // Returns true iff segments ab and cd intersect, properly or improperly.
        static bool Intersect (int a, int b, int c, int d, NativeArray<int> verts) {
            return default;
        }

        static bool Vequal (int a, int b, NativeArray<int> verts) {
            return default;
        }

        /// <summary>(i-1+n) % n assuming 0 <= i < n</summary>
        static int Prev (int i, int n) {
            return default;
        }

        /// <summary>(i+1) % n assuming 0 <= i < n</summary>
        static int Next (int i, int n) {
            return default;
        }

        static int AddVertex(NativeList<Int3> vertices, NativeHashMapInt3Int vertexMap, Int3 vertex)
        {
            return default;
        }

        public void Execute()
        {
        }

        void RemoveTileBorderVertices(ref VoxelMesh mesh, NativeArray<bool> verticesToRemove)
        {
        }

        bool CanRemoveVertex(ref VoxelMesh mesh, int vertexToRemove, UnsafeSpan<byte> vertexScratch)
        {
            return default;
        }

        void RemoveVertex(ref VoxelMesh mesh, int vertexToRemove)
        {
        }

        static int Triangulate(int n, NativeArray<int> verts, NativeArray<int> indices, NativeArray<int> tris)
        {
            return default;
        }
    }
}
