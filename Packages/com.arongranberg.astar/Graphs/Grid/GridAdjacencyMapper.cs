using Unity.Collections;

namespace Pathfinding.Graphs.Grid {
	public interface GridAdjacencyMapper {
		int LayerCount(IntBounds bounds);
		int GetNeighbourIndex(int nodeIndexXZ, int nodeIndex, int direction, NativeArray<ulong> nodeConnections, NativeArray<int> neighbourOffsets, int layerStride);
		bool HasConnection(int nodeIndex, int direction, NativeArray<ulong> nodeConnections);
	}

	public struct FlatGridAdjacencyMapper : GridAdjacencyMapper {
		public int LayerCount (IntBounds bounds) {
            return default;
        }

        public int GetNeighbourIndex(int nodeIndexXZ, int nodeIndex, int direction, NativeArray<ulong> nodeConnections, NativeArray<int> neighbourOffsets, int layerStride)
        {
            return default;
        }

        public bool HasConnection(int nodeIndex, int direction, NativeArray<ulong> nodeConnections)
        {
            return default;
        }
    }

	public struct LayeredGridAdjacencyMapper : GridAdjacencyMapper {
		public int LayerCount(IntBounds bounds) => bounds.size.y;
		public int GetNeighbourIndex (int nodeIndexXZ, int nodeIndex, int direction, NativeArray<ulong> nodeConnections, NativeArray<int> neighbourOffsets, int layerStride) {
            return default;
        }

        public bool HasConnection(int nodeIndex, int direction, NativeArray<ulong> nodeConnections)
        {
            return default;
        }
    }
}
