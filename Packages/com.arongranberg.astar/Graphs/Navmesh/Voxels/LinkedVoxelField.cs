using Pathfinding.Jobs;
using Unity.Collections;
using Unity.Mathematics;

namespace Pathfinding.Graphs.Navmesh.Voxelization.Burst {
	struct CellMinMax {
		public int objectID;
		public int min;
		public int max;
	}

	public struct LinkedVoxelField : IArenaDisposable {
		public const uint MaxHeight = 65536;
		public const int MaxHeightInt = 65536;
		/// <summary>
		/// Constant for default LinkedVoxelSpan top and bottom values.
		/// It is important with the U since ~0 != ~0U
		/// This can be used to check if a LinkedVoxelSpan is valid and not just the default span
		/// </summary>
		public const uint InvalidSpanValue = ~0U;

		/// <summary>Initial estimate on the average number of spans (layers) in the voxel representation. Should be greater or equal to 1</summary>
		public const float AvgSpanLayerCountEstimate = 8;

		/// <summary>The width of the field along the x-axis. [Limit: >= 0] [Units: voxels]</summary>
		public int width;

		/// <summary>The depth of the field along the z-axis. [Limit: >= 0] [Units: voxels]</summary>
		public int depth;
		/// <summary>The maximum height coordinate. [Limit: >= 0, <= MaxHeight] [Units: voxels]</summary>
		public int height;
		public bool flatten;

		public NativeList<LinkedVoxelSpan> linkedSpans;
		private NativeList<int> removedStack;
		private NativeList<CellMinMax> linkedCellMinMax;

		public LinkedVoxelField (int width, int depth, int height) : this()
        {
        }

        void IArenaDisposable.DisposeWith (DisposeArena arena) {
        }

        public void ResetLinkedVoxelSpans () {
        }

        void PushToSpanRemovedStack(int index)
        {
        }

        public int GetSpanCount()
        {
            return default;
        }

        public void ResolveSolid(int index, int objectID, int voxelWalkableClimb)
        {
        }

        public void SetWalkableBackground()
        {
        }

        public void AddFlattenedSpan(int index, int area)
        {
        }

        public void AddLinkedSpan (int index, int bottom, int top, int area, int voxelWalkableClimb, int objectID)
        {
        }
    }

    public struct LinkedVoxelSpan
    {
        public uint bottom;
        public uint top;

        public int next;

        /*Area
		 * 0 is an unwalkable span (triangle face down)
		 * 1 is a walkable span (triangle face up)
		 */
        public int area;

        public LinkedVoxelSpan(uint bottom, uint top, int area) : this()
        {
        }

        public LinkedVoxelSpan(uint bottom, uint top, int area, int next) : this()
        {
        }
    }
}
