#if !ASTAR_NO_GRID_GRAPH
using UnityEngine;
using System.Collections.Generic;
using Pathfinding.Serialization;
using Pathfinding.Graphs.Grid;

namespace Pathfinding {
	/// <summary>
	/// Grid Graph, supports layered worlds.
	/// [Open online documentation to see images]
	/// The GridGraph is great in many ways, reliable, easily configured and updatable during runtime.
	/// But it lacks support for worlds which have multiple layers, such as a building with multiple floors.
	/// That's where this graph type comes in. It supports basically the same stuff as the grid graph, but also multiple layers.
	/// It uses a bit more memory than a regular grid graph, but is otherwise equivalent.
	///
	/// See: get-started-grid (view in online documentation for working links)
	/// See: graphTypes (view in online documentation for working links)
	///
	/// \section layergridgraph-inspector Inspector
	/// [Open online documentation to see images]
	///
	/// \inspectorField{Shape, inspectorGridMode}
	/// \inspectorField{2D, is2D}
	/// \inspectorField{Align  to tilemap, AlignToTilemap}
	/// \inspectorField{Width, width}
	/// \inspectorField{Depth, depth}
	/// \inspectorField{Node size, nodeSize}
	/// \inspectorField{Aspect ratio (isometric/advanced shape), aspectRatio}
	/// \inspectorField{Isometric angle (isometric/advanced shape), isometricAngle}
	/// \inspectorField{Center, center}
	/// \inspectorField{Rotation, rotation}
	/// \inspectorField{Connections, neighbours}
	/// \inspectorField{Cut corners, cutCorners}
	/// \inspectorField{Max step height, maxStepHeight}
	/// \inspectorField{Account for slopes, maxStepUsesSlope}
	/// \inspectorField{Max slope, maxSlope}
	/// \inspectorField{Erosion iterations, erodeIterations}
	/// \inspectorField{Erosion → Erosion Uses Tags, erosionUseTags}
	/// \inspectorField{Use 2D physics, collision.use2D}
	///
	/// <b>Collision testing</b>
	/// \inspectorField{Enable Collision Testing, collision.collisionCheck}
	/// \inspectorField{Collider type, collision.type}
	/// \inspectorField{Diameter, collision.diameter}
	/// \inspectorField{Height/length, collision.height}
	/// \inspectorField{Offset, collision.collisionOffset}
	/// \inspectorField{Obstacle layer mask, collision.mask}
	/// \inspectorField{Preview, GridGraphEditor.collisionPreviewOpen}
	///
	/// <b>Height testing</b>
	/// \inspectorField{Enable Height Testing, collision.heightCheck}
	/// \inspectorField{Ray length, collision.fromHeight}
	/// \inspectorField{Mask, collision.heightMask}
	/// \inspectorField{Thick raycast, collision.thickRaycast}
	/// \inspectorField{Unwalkable when no ground, collision.unwalkableWhenNoGround}
	///
	/// <b>Rules</b>
	/// Take a look at grid-rules (view in online documentation for working links) for a list of available rules.
	///
	/// <b>Other settings</b>
	/// \inspectorField{Show surface, showMeshSurface}
	/// \inspectorField{Show outline, showMeshOutline}
	/// \inspectorField{Show connections, showNodeConnections}
	/// \inspectorField{Initial penalty, NavGraph.initialPenalty}
	///
	/// Note: The graph supports 16 layers by default, but it can be increased to 256 by enabling the ASTAR_LEVELGRIDNODE_MORE_LAYERS option in the A* Inspector → Settings → Optimizations tab.
	///
	/// See: <see cref="GridGraph"/>
	/// </summary>
	[Pathfinding.Util.Preserve]
	public class LayerGridGraph : GridGraph, IUpdatableGraph {
		// This function will be called when this graph is destroyed
		protected override void DisposeUnmanagedData () {
        }

        public LayerGridGraph () {
        }

        protected override GridNodeBase[] AllocateNodesJob (int size, out Unity.Jobs.JobHandle dependency) {
            dependency = default(Unity.Jobs.JobHandle);
            return default;
        }

        /// <summary>
        /// Number of layers.
        /// Warning: Do not modify this variable
        /// </summary>
        [JsonMember]
		internal int layerCount;

		/// <summary>Nodes with a short distance to the node above it will be set unwalkable</summary>
		[JsonMember]
		public float characterHeight = 0.4F;

		internal int lastScannedWidth;
		internal int lastScannedDepth;

		public override int LayerCount {
			get => layerCount;
			protected set => layerCount = value;
		}

		public override int MaxLayers => LevelGridNode.MaxLayerCount;

		public override int CountNodes () {
            return default;
        }

        public override void GetNodes (System.Action<GraphNode> action) {
        }

        /// <summary>
        /// Get all nodes in a rectangle.
        /// Returns: The number of nodes written to the buffer.
        /// </summary>
        /// <param name="rect">Region in which to return nodes. It will be clamped to the grid.</param>
        /// <param name="buffer">Buffer in which the nodes will be stored. Should be at least as large as the number of nodes that can exist in that region.</param>
        public override int GetNodesInRegion (IntRect rect, GridNodeBase[] buffer) {
            return default;
        }

        /// <summary>
        /// Node in the specified cell.
        /// Returns null if the coordinate is outside the grid.
        ///
        /// If you know the coordinate is inside the grid and you are looking to maximize performance then you
        /// can look up the node in the internal array directly which is slightly faster.
        /// See: <see cref="nodes"/>
        /// </summary>
        public GridNodeBase GetNode (int x, int z, int layer) {
            return default;
        }

        protected override IGraphUpdatePromise ScanInternal (bool async) {
            return default;
        }

        protected override GridNodeBase GetNearestFromGraphSpace(Vector3 positionGraphSpace)
        {
            return default;
        }

        private GridNodeBase GetNearestNode(Vector3 position, int x, int z, NNConstraint constraint)
        {
            return default;
        }

        protected override void SerializeExtraInfo(GraphSerializationContext ctx)
        {
        }

        protected override void DeserializeExtraInfo(GraphSerializationContext ctx)
        {
        }

        protected override void PostDeserialization(GraphSerializationContext ctx)
        {
        }
    }
}
#endif
