using UnityEngine;
using Unity.Collections;
using Unity.Mathematics;
using Unity.Jobs;
using Unity.Burst;

namespace Pathfinding.Graphs.Navmesh.Voxelization.Burst {
	using Pathfinding.Util;
	using Unity.Collections.LowLevel.Unsafe;
	using Pathfinding.Collections;

	public struct RasterizationMesh {
		public UnsafeSpan<float3> vertices;

		public UnsafeSpan<int> triangles;

		public int area;

		/// <summary>World bounds of the mesh. Assumed to already be multiplied with the matrix</summary>
		public Bounds bounds;

		public Matrix4x4 matrix;

		/// <summary>
		/// If true then the mesh will be treated as solid and its interior will be unwalkable.
		/// The unwalkable region will be the minimum to maximum y coordinate in each cell.
		/// </summary>
		public bool solid;

		/// <summary>If true, both sides of the mesh will be walkable. If false, only the side that the normal points towards will be walkable</summary>
		public bool doubleSided;

		/// <summary>If true, the <see cref="area"/> will be interpreted as a node tag and applied to the final nodes</summary>
		public bool areaIsTag;

		/// <summary>
		/// If true, the mesh will be flattened to the base of the graph during rasterization.
		///
		/// This is intended for rasterizing 2D meshes which always lie in a single plane.
		///
		/// This will also cause unwalkable spans have precedence over walkable ones at all times, instead of
		/// only when the unwalkable span is sufficiently high up over a walkable span. Since when flattening,
		/// "sufficiently high up" makes no sense.
		/// </summary>
		public bool flatten;
	}

	[BurstCompile(CompileSynchronously = true)]
	public struct JobVoxelize : IJob {
		[ReadOnly]
		public NativeArray<RasterizationMesh> inputMeshes;

		[ReadOnly]
		public NativeArray<int> bucket;

		/// <summary>Maximum ledge height that is considered to still be traversable. [Limit: >=0] [Units: vx]</summary>
		public int voxelWalkableClimb;

		/// <summary>
		/// Minimum floor to 'ceiling' height that will still allow the floor area to
		/// be considered walkable. [Limit: >= 3] [Units: vx]
		/// </summary>
		public uint voxelWalkableHeight;

		/// <summary>The xz-plane cell size to use for fields. [Limit: > 0] [Units: wu]</summary>
		public float cellSize;

		/// <summary>The y-axis cell size to use for fields. [Limit: > 0] [Units: wu]</summary>
		public float cellHeight;

		/// <summary>The maximum slope that is considered walkable. [Limits: 0 <= value < 90] [Units: Degrees]</summary>
		public float maxSlope;

		public Matrix4x4 graphTransform;
		public Bounds graphSpaceBounds;
		public Vector2 graphSpaceLimits;
		public LinkedVoxelField voxelArea;

		public void Execute ()
        {
        }
    }

    [BurstCompile(CompileSynchronously = true)]
    struct JobBuildCompactField : IJob
    {
        public LinkedVoxelField input;
        public CompactVoxelField output;

        public void Execute()
        {
        }
    }


    [BurstCompile(CompileSynchronously = true)]
    struct JobBuildConnections : IJob
    {
        public CompactVoxelField field;
        public int voxelWalkableHeight;
        public int voxelWalkableClimb;

        public void Execute()
        {
        }
    }

    [BurstCompile(CompileSynchronously = true)]
    struct JobErodeWalkableArea : IJob
    {
        public CompactVoxelField field;
        public int radius;

        public void Execute()
        {
        }
    }

    [BurstCompile(CompileSynchronously = true)]
    struct JobBuildDistanceField : IJob
    {
        public CompactVoxelField field;
        public NativeList<ushort> output;

        public void Execute()
        {
        }
    }

    [BurstCompile(CompileSynchronously = true)]
    struct JobFilterLowHeightSpans : IJob
    {
        public LinkedVoxelField field;
        public uint voxelWalkableHeight;

        public void Execute()
        {
        }
    }

    [BurstCompile(CompileSynchronously = true)]
    struct JobFilterLedges : IJob
    {
        public LinkedVoxelField field;
        public uint voxelWalkableHeight;
        public int voxelWalkableClimb;
        public float cellSize;
        public float cellHeight;

        // Code almost completely ripped from Recast
        public void Execute()
        {
        }
    }
}
