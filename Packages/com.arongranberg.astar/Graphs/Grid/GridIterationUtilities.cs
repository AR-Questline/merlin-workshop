using Pathfinding.Jobs;
using Unity.Collections;
using Unity.Mathematics;
using UnityEngine.Assertions;

namespace Pathfinding.Graphs.Grid {
	/// <summary>
	/// Helpers for iterating over grid graph data.
	///
	/// This is a helper for iterating over grid graph data, which is typically stored in an array of size width*layers*depth (x*y*z).
	/// It is used internally by grid graph jobs, and can also be used by custom grid graph rules.
	///
	/// See: grid-rules-write (view in online documentation for working links)
	/// </summary>
	public static class GridIterationUtilities {
		/// <summary>Callback struct for <see cref="ForEachCellIn3DSlice"/></summary>
		public interface ISliceAction {
			void Execute(uint outerIdx, uint innerIdx);
		}

		/// <summary>Callback struct for <see cref="ForEachCellIn3DSlice"/></summary>
		public interface ISliceActionWithCoords {
			void Execute(uint outerIdx, uint innerIdx, int3 innerCoords);
		}

		/// <summary>Callback struct for <see cref="ForEachCellIn3DArray"/></summary>
		public interface ICellAction {
			void Execute(uint idx, int x, int y, int z);
		}

		/// <summary>
		/// Iterates over a slice of a 3D array.
		///
		/// This is a helper for iterating over grid graph data, which is typically stored in an array of size width*layers*depth (x*y*z).
		///
		/// In burst-compiled code, this will be essentially as fast as writing the loop code yourself. In C#, it is marginally slower than writing the loop code yourself.
		///
		/// [Open online documentation to see images]
		/// </summary>
		/// <param name="slice">Bounds of the slice and the size of the outer array it is relative to.</param>
		/// <param name="action">Your callback struct. The Execute method on the callback struct will be called for each element in the slice. It will be passed both the index in the slice and the index in the outer array.</param>
		public static void ForEachCellIn3DSlice<T>(Slice3D slice, ref T action) where T : struct, ISliceAction {
        }

        /// <summary>
        /// Iterates over a slice of a 3D array.
        ///
        /// This is a helper for iterating over grid graph data, which is typically stored in an array of size width*layers*depth (x*y*z).
        ///
        /// In burst-compiled code, this will be essentially as fast as writing the loop code yourself. In C#, it is marginally slower than writing the loop code yourself.
        ///
        /// [Open online documentation to see images]
        /// </summary>
        /// <param name="slice">Bounds of the slice and the size of the outer array it is relative to.</param>
        /// <param name="action">Your callback struct. The Execute method on the callback struct will be called for each element in the slice. It will be passed both the index in the slice and the index in the outer array.</param>
        public static void ForEachCellIn3DSliceWithCoords<T>(Slice3D slice, ref T action) where T : struct, ISliceActionWithCoords
        {
        }

        /// <summary>
        /// Iterates over a 3D array.
        ///
        /// This is a helper for iterating over grid graph data, which is typically stored in an array of size width*layers*depth (x*y*z).
        ///
        /// In burst-compiled code, this will be essentially as fast as writing the loop code yourself. In C#, it is marginally slower than writing the loop code yourself.
        /// </summary>
        /// <param name="size">Size of the array.</param>
        /// <param name="action">Your callback struct. The Execute method on the callback struct will be called for each element in the array. It will be passed the x, y and z coordinates of the element as well as the index in the array.</param>
        public static void ForEachCellIn3DArray<T>(int3 size, ref T action) where T : struct, ICellAction
        {
        }

        /// <summary>
        /// Helper interface for modifying nodes.
        /// This is used by the <see cref="GridIterationUtilities.ForEachNode"/> function.
        /// </summary>
        public interface INodeModifier
        {
            /// <summary>
            /// Called for every node that is being updated.
            ///
            /// See: gridgraphrule-burst (view in online documentation for working links) for example usage.
            /// </summary>
            /// <param name="dataIndex">Index of the node. This is the index in the data arrays for the graph update, not necessarily the index in the graph.</param>
            /// <param name="dataX">X coordinate of the node, relative to the updated region.</param>
            /// <param name="dataLayer">Layer (Y) coordinate of the node, relative to the updated region.</param>
            /// <param name="dataZ">Z coordinate of the node, relative to the updated region.</param>
            void ModifyNode(int dataIndex, int dataX, int dataLayer, int dataZ);
        }

        /// <summary>
        /// Iterate through all nodes that exist.
        ///
        /// See: grid-rules-write (view in online documentation for working links) for example usage.
        /// </summary>
        /// <param name="arrayBounds">Size of the rectangle of the grid graph that is being updated/scanned</param>
        /// <param name="nodeNormals">Data for all node normals. This is used to determine if a node exists (important for layered grid graphs).</param>
        /// <param name="callback">The ModifyNode method on the callback struct will be called for each node.</param>
        public static void ForEachNode<T>(int3 arrayBounds, NativeArray<float4> nodeNormals, ref T callback) where T : struct, INodeModifier
        {
        }

        /// <summary>
        /// Helper interface for modifying node connections.
        /// This is used by the <see cref="GridIterationUtilities.FilterNodeConnections"/> function.
        /// </summary>
        public interface IConnectionFilter {
			/// <summary>
			/// Returns true if the connection should be enabled.
			///
			/// See: gridgraphrule-connection-filter (view in online documentation for working links) for example usage.
			/// See: <see cref="GridIterationUtilities.GetNeighbourDataIndex"/>
			/// </summary>
			/// <param name="dataIndex">Index of the node for which the connection is being tested. This is the index in the data arrays for the graph update, not necessarily the index in the graph.</param>
			/// <param name="dataX">X coordinate of the node for which the connection is being tested, relative to the updated region.</param>
			/// <param name="dataLayer">Layer (Y) coordinate of the node for which the connection is being tested, relative to the updated region.</param>
			/// <param name="dataZ">Z coordinate of the node for which the connection is being tested, relative to the updated region.</param>
			/// <param name="direction">Direction to the neighbour. See \reflink{GridNode.HasConnectionInDirection}.</param>
			/// <param name="neighbourDataIndex">Index of the neighbour node. This is the index in the data arrays for the graph update, not necessarily the index in the graph.</param>
			bool IsValidConnection(int dataIndex, int dataX, int dataLayer, int dataZ, int direction, int neighbourDataIndex);
		}

		/// <summary>
		/// Iterate through all enabled connections of all nodes.
		///
		/// See: grid-rules-write (view in online documentation for working links) for example usage.
		/// </summary>
		/// <param name="bounds">Sub-rectangle of the grid graph that is being updated/scanned</param>
		/// <param name="nodeConnections">Data with all node connections.</param>
		/// <param name="layeredDataLayout">Should be true for layered grid graphs and false otherwise.</param>
		/// <param name="filter">Your callback struct. The IsValidConnection method on the callback struct will be called for each connection. If false is returned, the connection will be disabled.</param>
		public static void FilterNodeConnections<T>(IntBounds bounds, NativeArray<ulong> nodeConnections, bool layeredDataLayout, ref T filter) where T : struct, IConnectionFilter {
        }

        /// <summary>
        /// Returns the data index for a node's neighbour in the given direction.
        ///
        /// The bounds, nodeConnections and layeredDataLayout fields can be retrieved from the <see cref="GridGraphRules.Context"/>.data object.
        ///
        /// Returns: Null if the node has no connection in that direction. Otherwise the data index for that node is returned.
        ///
        /// See: gridgraphrule-connection-filter (view in online documentation for working links) for example usage.
        /// </summary>
        /// <param name="bounds">Sub-rectangle of the grid graph that is being updated/scanned</param>
        /// <param name="nodeConnections">Data for all node connections</param>
        /// <param name="layeredDataLayout">True if this is a layered grid graph</param>
        /// <param name="dataX">X coordinate in the data arrays for the node for which you want to get a neighbour</param>
        /// <param name="dataLayer">Layer (Y) coordinate in the data arrays for the node for which you want to get a neighbour</param>
        /// <param name="dataZ">Z coordinate in the data arrays for the node for which you want to get a neighbour</param>
        /// <param name="direction">Direction to the neighbour. See \reflink{GridNode.HasConnectionInDirection}.</param>
        public static int? GetNeighbourDataIndex (IntBounds bounds, NativeArray<ulong> nodeConnections, bool layeredDataLayout, int dataX, int dataLayer, int dataZ, int direction) {
            return default;
        }
    }
}
