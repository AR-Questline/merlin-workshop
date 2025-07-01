using UnityEngine;
using Unity.Collections;
using Unity.Burst;
using Unity.Jobs;
using Pathfinding.Collections;
using Unity.Mathematics;

namespace Pathfinding.Util {
#if MODULE_COLLECTIONS_2_1_0_OR_NEWER
	using NativeHashMapInt3Int = Unity.Collections.NativeHashMap<Int3, int>;
#else
	using NativeHashMapInt3Int = Unity.Collections.NativeParallelHashMap<Int3, int>;
#endif

	/// <summary>Helper class for working with meshes efficiently</summary>
	[BurstCompile]
	static class MeshUtility {
		public static void GetMeshData (Mesh.MeshDataArray meshData, int meshIndex, in WaterProperties waterProperties, out NativeArray<Vector3> vertices, out NativeArray<int> indices) {
            vertices = default(NativeArray<Vector3>);
            indices = default(NativeArray<int>);
        }

        /// <summary>
        /// Flips triangles such that they are all clockwise in graph space.
        ///
        /// The triangles may not be clockwise in world space since the graphs can be rotated.
        ///
        /// The triangles array will be modified in-place.
        /// </summary>
        [BurstCompile]
        public static void MakeTrianglesClockwise(ref UnsafeSpan<Int3> vertices, ref UnsafeSpan<int> triangles)
        {
        }

        /// <summary>Removes duplicate vertices from the array and updates the triangle array.</summary>
        [BurstCompile]
		public struct JobRemoveDuplicateVertices : IJob {
			public NativeList<Int3> vertices;
			public NativeList<int> triangles;
			public NativeList<int> tags;

			public static int3 cross(int3 x, int3 y) => (x * y.yzx - x.yzx * y).yzx;

			public void Execute () {
            }
        }
	}
}
