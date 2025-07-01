using Pathfinding.Jobs;
using Unity.Collections;
using Unity.Mathematics;
using UnityEngine.Assertions;

namespace Pathfinding.Graphs.Navmesh.Voxelization.Burst {
	/// <summary>Stores a compact voxel field. </summary>
	public struct CompactVoxelField : IArenaDisposable {
		public const int UnwalkableArea = 0;
		public const uint NotConnected = 0x3f;
		public readonly int voxelWalkableHeight;
		public readonly int width, depth;
		public NativeList<CompactVoxelSpan> spans;
		public NativeList<CompactVoxelCell> cells;
		public NativeList<int> areaTypes;

		/// <summary>Unmotivated variable, but let's clamp the layers at 65535</summary>
		public const int MaxLayers = 65535;

		public CompactVoxelField (int width, int depth, int voxelWalkableHeight, Allocator allocator) : this()
        {
        }

        void IArenaDisposable.DisposeWith(DisposeArena arena)
        {
        }

        public int GetNeighbourIndex(int index, int direction)
        {
            return default;
        }

        public void BuildFromLinkedField(LinkedVoxelField field)
        {
        }
    }

    /// <summary>CompactVoxelCell used for recast graphs.</summary>
    public struct CompactVoxelCell
    {
        public int index;
        public int count;

        public CompactVoxelCell(int i, int c) : this()
        {
        }
    }

    /// <summary>CompactVoxelSpan used for recast graphs.</summary>
    public struct CompactVoxelSpan {
		public ushort y;
		public uint con;
		public uint h;
		public int reg;

		public CompactVoxelSpan (ushort bottom, uint height) : this()
        {
        }

        public void SetConnection (int dir, uint value) {
        }

        public int GetConnection (int dir) {
            return default;
        }
    }
}
