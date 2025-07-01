using UnityEngine;
using System.Collections.Generic;
using UnityEngine.Profiling;
using Unity.Collections;
using Unity.Mathematics;
using Unity.Burst;
using UnityEngine.Assertions;

namespace Pathfinding {
	using System.IO;
	using Pathfinding.Util;
	using Pathfinding.Serialization;
	using Math = System.Math;
	using System.Linq;
	using Pathfinding.Drawing;
	using Pathfinding.Graphs.Navmesh;
	using Pathfinding.Collections;
	using Pathfinding.Pooling;
	using Unity.Collections.LowLevel.Unsafe;

	/// <summary>Base class for <see cref="RecastGraph"/> and <see cref="NavMeshGraph"/></summary>
	[BurstCompile]
	public abstract class NavmeshBase : NavGraph, INavmeshHolder, ITransformedGraph
		, IRaycastableGraph {
#if ASTAR_RECAST_LARGER_TILES
		// Larger tiles
		public const int VertexIndexMask = 0xFFFFF;

		public const int TileIndexMask = 0x7FF;
		public const int TileIndexOffset = 20;
#else
		// Larger worlds
		public const int VertexIndexMask = 0xFFF;

		public const int TileIndexMask = 0x7FFFF;
		public const int TileIndexOffset = 12;
#endif
		/// <summary>Size of the bounding box.</summary>
		[JsonMember]
		public Vector3 forcedBoundsSize = new Vector3(100, 40, 100);

		public abstract float NavmeshCuttingCharacterRadius { get; }

		/// <summary>Size of a tile in world units along the X axis</summary>
		public abstract float TileWorldSizeX { get; }

		/// <summary>Size of a tile in world units along the Z axis</summary>
		public abstract float TileWorldSizeZ { get; }

		/// <summary>
		/// Maximum (vertical) distance between the sides of two nodes for them to be connected across a tile edge.
		/// When tiles are connected to each other, the nodes sometimes do not line up perfectly
		/// so some allowance must be made to allow tiles that do not match exactly to be connected with each other.
		/// </summary>
		public abstract float MaxTileConnectionEdgeDistance { get; }

		/// <summary>
		/// Show an outline of the polygons in the Unity Editor.
		///
		/// [Open online documentation to see images]
		/// </summary>
		[JsonMember]
		public bool showMeshOutline = true;

		/// <summary>
		/// Show the connections between the polygons in the Unity Editor.
		///
		/// [Open online documentation to see images]
		/// </summary>
		[JsonMember]
		public bool showNodeConnections;

		/// <summary>
		/// Show the surface of the navmesh.
		///
		/// [Open online documentation to see images]
		/// </summary>
		[JsonMember]
		public bool showMeshSurface = true;

		/// <summary>Number of tiles along the X-axis</summary>
		public int tileXCount;
		/// <summary>Number of tiles along the Z-axis</summary>
		public int tileZCount;

		/// <summary>
		/// All tiles.
		///
		/// See: <see cref="GetTile"/>
		/// </summary>
		protected NavmeshTile[] tiles;

		/// <summary>
		/// Perform nearest node searches in XZ space only.
		/// Recomended for single-layered environments. Faster but can be inaccurate esp. in multilayered contexts.
		/// You should not use this if the graph is rotated since then the XZ plane no longer corresponds to the ground plane.
		///
		/// This can be important on sloped surfaces. See the image below in which the closest point for each blue point is queried for:
		/// [Open online documentation to see images]
		///
		/// You can also control this using a <see cref="Pathfinding.NNConstraint.distanceXZ field on an NNConstraint"/>.
		///
		/// Deprecated: Set the appropriate fields on the NNConstraint instead.
		/// </summary>
		[JsonMember]
		[System.Obsolete("Set the appropriate fields on the NNConstraint instead")]
		public bool nearestSearchOnlyXZ;

		/// <summary>
		/// Should navmesh cuts affect this graph.
		/// See: <see cref="navmeshUpdateData"/>
		/// </summary>
		[JsonMember]
		public bool enableNavmeshCutting = true;

		/// <summary>
		/// Handles navmesh cutting.
		/// See: <see cref="enableNavmeshCutting"/>
		/// See: <see cref="NavmeshUpdates"/>
		/// </summary>
		public NavmeshUpdates.NavmeshUpdateSettings navmeshUpdateData;

		/// <summary>Positive if currently updating tiles in a batch</summary>
		int batchTileUpdate;
		/// <summary>True if the current batch of tile updates requires navmesh cutting to be done</summary>
		bool batchPendingNavmeshCutting;

		/// <summary>List of tiles updating during batch</summary>
		List<int> batchUpdatedTiles = new List<int>();

		/// <summary>List of nodes that are going to be destroyed as part of a batch update</summary>
		List<MeshNode> batchNodesToDestroy = new List<MeshNode>();

		/// <summary>
		/// Determines how the graph transforms graph space to world space.
		/// See: <see cref="CalculateTransform"/>
		///
		/// Warning: Do not modify this directly, instead use e.g. <see cref="RelocateNodes(GraphTransform)"/>
		/// </summary>
		public GraphTransform transform = GraphTransform.identityTransform;

		GraphTransform ITransformedGraph.transform { get { return transform; } }

		/// <summary>\copydoc Pathfinding::NavMeshGraph::recalculateNormals</summary>
		public abstract bool RecalculateNormals { get; }

		public override bool isScanned => tiles != null;

		/// <summary>
		/// Returns a new transform which transforms graph space to world space.
		/// Does not update the <see cref="transform"/> field.
		/// See: <see cref="RelocateNodes(GraphTransform)"/>
		/// </summary>
		public abstract GraphTransform CalculateTransform();

		/// <summary>
		/// Called when tiles have been completely recalculated.
		/// This is called after scanning the graph and after
		/// performing graph updates that completely recalculate tiles
		/// (not ones that simply modify e.g penalties).
		/// It is not called after NavmeshCut updates.
		/// </summary>
		public System.Action<NavmeshTile[]> OnRecalculatedTiles;

		/// <summary>
		/// Tile at the specified x, z coordinate pair.
		/// The first tile is at (0,0), the last tile at (tileXCount-1, tileZCount-1).
		///
		/// <code>
		/// var graph = AstarPath.active.data.recastGraph;
		/// int tileX = 5;
		/// int tileZ = 8;
		/// NavmeshTile tile = graph.GetTile(tileX, tileZ);
		///
		/// for (int i = 0; i < tile.nodes.Length; i++) {
		///     // ...
		/// }
		/// // or you can access the nodes like this:
		/// tile.GetNodes(node => {
		///     // ...
		/// });
		/// </code>
		/// </summary>
		public NavmeshTile GetTile (int x, int z) {
            return default;
        }

        /// <summary>
        /// Vertex coordinate for the specified vertex index.
        ///
        /// Throws: IndexOutOfRangeException if the vertex index is invalid.
        /// Throws: NullReferenceException if the tile the vertex is in is not calculated.
        ///
        /// See: NavmeshTile.GetVertex
        /// </summary>
        public Int3 GetVertex (int index) {
            return default;
        }

        /// <summary>Vertex coordinate in graph space for the specified vertex index</summary>
        public Int3 GetVertexInGraphSpace (int index) {
            return default;
        }

        /// <summary>Tile index from a vertex index</summary>
        public static int GetTileIndex (int index) {
            return default;
        }

        public int GetVertexArrayIndex (int index) {
            return default;
        }

        /// <summary>Tile coordinates from a tile index</summary>
        public void GetTileCoordinates (int tileIndex, out int x, out int z) {
            x = default(int);
            z = default(int);
        }

        /// <summary>
        /// All tiles.
        /// Warning: Do not modify this array
        /// </summary>
        public NavmeshTile[] GetTiles () {
            return default;
        }

        /// <summary>
        /// Returns a bounds object with the bounding box of a group of tiles.
        ///
        /// The bounding box is defined in world space.
        /// </summary>
        /// <param name="rect">Tiles to get the bounding box of. The rectangle is in tile coordinates where 1 unit = 1 tile.</param>
        public Bounds GetTileBounds (IntRect rect) {
            return default;
        }

        /// <summary>
        /// Returns a bounds object with the bounding box of a group of tiles.
        /// The bounding box is defined in world space.
        /// </summary>
        public Bounds GetTileBounds (int x, int z, int width = 1, int depth = 1) {
            return default;
        }

        /// <summary>Returns an XZ bounds object with the bounds of a group of tiles in graph space.</summary>
        /// <param name="rect">Tiles to get the bounding box of. The rectangle is in tile coordinates where 1 unit = 1 tile.</param>
        public Bounds GetTileBoundsInGraphSpace (IntRect rect) {
            return default;
        }

        /// <summary>Returns an XZ bounds object with the bounds of a group of tiles in graph space</summary>
        public Bounds GetTileBoundsInGraphSpace (int x, int z, int width = 1, int depth = 1) {
            return default;
        }

        /// <summary>
        /// Returns the tile coordinate which contains the specified position.
        /// It is not necessarily a valid tile (i.e it could be out of bounds).
        /// </summary>
        public Vector2Int GetTileCoordinates (Vector3 position) {
            return default;
        }

        protected override void OnDestroy () {
        }

        protected override void DisposeUnmanagedData () {
        }

        protected override void DestroyAllNodes () {
        }

        public override void RelocateNodes (Matrix4x4 deltaMatrix) {
        }

        /// <summary>
        /// Moves the nodes in this graph.
        /// Moves all the nodes in such a way that the specified transform is the new graph space to world space transformation for the graph.
        /// You usually use this together with the <see cref="CalculateTransform"/> method.
        ///
        /// So for example if you want to move and rotate all your nodes in e.g a recast graph you can do
        /// <code>
        /// AstarPath.active.AddWorkItem(() => {
        ///     // Move the graph to the point (20, 10, 10), rotated 45 degrees around the X axis
        ///     var graph = AstarPath.active.data.recastGraph;
        ///     graph.forcedBoundsCenter = new Vector3(20, 10, 10);
        ///     graph.rotation = new Vector3(45, 0, 0);
        ///     graph.RelocateNodes(graph.CalculateTransform());
        /// });
        /// </code>
        ///
        /// For a navmesh graph it will look like:
        /// * <code>
        /// AstarPath.active.AddWorkItem((System.Action)(() => {
        ///     // Move the graph to the point (20, 10, 10), rotated 45 degrees around the X axis
        ///     var graph = AstarPath.active.data.navmeshGraph;
        ///     graph.offset = new Vector3(20, 10, 10);
        ///     graph.rotation = new Vector3(45, 0, 0);
        ///     graph.RelocateNodes((GraphTransform)graph.CalculateTransform());
        /// }));
        /// </code>
        ///
        /// This will move all the nodes to new positions as if the new graph settings had been there from the start.
        ///
        /// Note: RelocateNodes(deltaMatrix) is not equivalent to RelocateNodes(new GraphTransform(deltaMatrix)).
        ///  The overload which takes a matrix multiplies all existing node positions with the matrix while this
        ///  overload does not take into account the current positions of the nodes.
        ///
        /// See: <see cref="CalculateTransform"/>
        /// </summary>
        public void RelocateNodes (GraphTransform newTransform) {
        }

        /// <summary>Creates a single new empty tile</summary>
        protected NavmeshTile NewEmptyTile (int x, int z) {
            return default;
        }

        public override void GetNodes (System.Action<GraphNode> action) {
        }

        /// <summary>
        /// Returns a rect containing the indices of all tiles touching the specified bounds.
        /// If a margin is passed, the bounding box in graph space is expanded by that amount in every direction.
        /// </summary>
        public IntRect GetTouchingTiles (Bounds bounds, float margin = 0) {
            return default;
        }

        /// <summary>Returns a rect containing the indices of all tiles touching the specified bounds.</summary>
        /// <param name="rect">Graph space rectangle (in graph space all tiles are on the XZ plane regardless of graph rotation and other transformations, the first tile has a corner at the origin)</param>
        public IntRect GetTouchingTilesInGraphSpace (Rect rect) {
            return default;
        }

        protected void ConnectTileWithNeighbours(NavmeshTile tile, bool onlyUnflagged = false)
        {
        }

        public override float NearestNodeDistanceSqrLowerBound(Vector3 position, NNConstraint constraint)
        {
            return default;
        }

        public override NNInfo GetNearest (Vector3 position, NNConstraint constraint, float maxDistanceSqr) {
            return default;
        }

        public override NNInfo RandomPointOnSurface(NNConstraint nnConstraint = null, bool highQuality = true)
        {
            return default;
        }

        /// <summary>
        /// Finds the first node which contains position.
        /// "Contains" is defined as position is inside the triangle node when seen from above.
        /// In case of a multilayered environment, the closest node which contains the point is returned.
        ///
        /// Returns null if there was no node containing the point. This serves as a quick
        /// check for "is this point on the navmesh or not".
        ///
        /// Note that the behaviour of this method is distinct from the GetNearest method.
        /// The GetNearest method will return the closest node to a point,
        /// which is not necessarily the one which contains it when seen from above.
        ///
        /// Uses <see cref="NNConstraint.distanceMetric"/> to define the "up" direction. The up direction of the graph will be used if it is not set.
        /// The up direction defines what "inside" a node means. A point is inside a node if it is inside the triangle when seen from above.
        ///
        /// See: <see cref="GetNearest"/>
        ///
        /// See: <see cref="IsPointOnNavmesh"/>, if you only need to know if the point is on the navmesh or not.
        /// </summary>
        public GraphNode PointOnNavmesh(Vector3 position, NNConstraint constraint)
        {
            return default;
        }

        /// <summary>Fills graph with tiles created by NewEmptyTile</summary>
        protected void FillWithEmptyTiles()
        {
        }

        /// <summary>Create connections between all nodes</summary>
        protected static void CreateNodeConnections(TriangleMeshNode[] nodes, bool keepExistingConnections)
        {
        }

        /// <summary>
        /// Generate connections between the two tiles.
        /// The tiles must be adjacent.
        /// </summary>
        internal static void ConnectTiles(NavmeshTile tile1, NavmeshTile tile2, float tileWorldSizeX, float tileWorldSizeZ, float maxTileConnectionEdgeDistance)
        {
        }

        /// <summary>
        /// Start batch updating of tiles.
        /// During batch updating, tiles will not be connected if they are updating with ReplaceTile.
        /// When ending batching, all affected tiles will be connected.
        /// This is faster than not using batching.
        ///
        /// Batching can be nested, but the <see cref="EndBatchTileUpdate"/> method must be called the same number of times as StartBatchTileUpdate.
        /// </summary>
        /// <param name="exclusive">If true, an exception will be thrown if batching is already enabled.</param>
        public void StartBatchTileUpdate(bool exclusive = false)
        {
        }

        /// <summary>
        /// Destroy several nodes simultaneously.
        /// This is faster than simply looping through the nodes and calling the node.Destroy method because some optimizations
        /// relating to how connections are removed can be optimized.
        /// </summary>
        static void DestroyNodes(List<MeshNode> nodes)
        {
        }

        void TryConnect(int tileIdx1, int tileIdx2)
        {
        }

        /// <summary>
        /// End batch updating of tiles.
        /// During batch updating, tiles will not be connected if they are updated with ReplaceTile.
        /// When ending batching, all affected tiles will be connected.
        /// This is faster than not using batching.
        /// </summary>
        public void EndBatchTileUpdate()
        {
        }

        /// <summary>Clears the tiles in the specified rectangle.</summary>
        /// <param name="tileRect">The rectangle in tile coordinates to clear. The coordinates are in tile coordinates, not world coordinates.</param>
        public void ClearTiles(IntRect tileRect)
        {
        }

        public void MarkToUpdate(NavmeshClipper clipper)
        {
        }

        /// <summary>
        /// Clear the tile at the specified coordinate.
        /// Must be called during a batch update, see <see cref="StartBatchTileUpdate"/>.
        /// </summary>
        protected void ClearTile(int x, int z, NavmeshTile replacement)
        {
        }

        /// <summary>Temporary buffer used in <see cref="PrepareNodeRecycling"/></summary>
        Dictionary<int, int> nodeRecyclingHashBuffer = new Dictionary<int, int>();

        /// <summary>
        /// Reuse nodes that keep the exact same vertices after a tile replacement.
        /// The reused nodes will be added to the recycledNodeBuffer array at the index corresponding to the
        /// indices in the triangle array that its vertices uses.
        ///
        /// All connections on the reused nodes will be removed except ones that go to other graphs.
        /// The reused nodes will be removed from the tile by replacing it with a null slot in the node array.
        ///
        /// See: <see cref="ReplaceTile"/>
        /// </summary>
        void PrepareNodeRecycling(int x, int z, UnsafeSpan<Int3> verts, UnsafeSpan<int> tris, TriangleMeshNode[] recycledNodeBuffer)
        {
        }

        /// <summary>
        /// Replace tile at index with nodes created from specified navmesh.
        /// This will create new nodes and link them to the adjacent tile (unless batching has been started in which case that will be done when batching ends).
        ///
        /// See: <see cref="StartBatchTileUpdate"/>
        /// </summary>
        /// <param name="x">X coordinate of the tile to replace.</param>
        /// <param name="z">Z coordinate of the tile to replace.</param>
        /// <param name="verts">Vertices of the new tile. The vertices are assumed to be in 'tile space', that is being in a rectangle with one corner at the origin and one at (#TileWorldSizeX, 0, #TileWorldSizeZ).</param>
        /// <param name="tris">Triangles of the new tile. If #RecalculateNormals is enabled, the triangles will be converted to clockwise order (when seen from above), if they are not already.</param>
        /// <param name="tags">Tags for the nodes. The array must either be null, or have the same length as the tris array divided by 3. If null, the tag will be set to 0 for all nodes.</param>
        /// <param name="tryPreserveExistingTagsAndPenalties">If true, existing tags and penalties will be preserved for nodes that stay in exactly the same position after the tile replacement.</param>
        public void ReplaceTile(int x, int z, Int3[] verts, int[] tris, uint[] tags = null, bool tryPreserveExistingTagsAndPenalties = true)
        {
        }

        /// <summary>
        /// Replace tile at index with nodes created from specified navmesh.
        /// This will create new nodes and link them to the adjacent tile (unless batching has been started in which case that will be done when batching ends).
        ///
        /// If there are <see cref="NavmeshCut"/> components in the scene, they will be applied to the tile.
        ///
        /// See: <see cref="StartBatchTileUpdate"/>
        /// </summary>
        /// <param name="x">X coordinate of the tile to replace.</param>
        /// <param name="z">Z coordinate of the tile to replace.</param>
        /// <param name="verts">Vertices of the new tile. The vertices are assumed to be in 'tile space', that is being in a rectangle with one corner at the origin and one at (#TileWorldSizeX, 0, #TileWorldSizeZ).</param>
        /// <param name="tris">Triangles of the new tile. If #RecalculateNormals is enabled, the triangles will be converted to clockwise order (when seen from above), if they are not already.</param>
        /// <param name="tags">Tags for the nodes. The array must either be empty, or have the same length as the tris array divided by 3. If empty, the tag will be set to 0 for all nodes.</param>
        /// <param name="tryPreserveExistingTagsAndPenalties">If true, existing tags and penalties will be preserved for nodes that stay in exactly the same position after the tile replacement.</param>
        public void ReplaceTile(int x, int z, UnsafeSpan<Int3> verts, UnsafeSpan<int> tris, UnsafeSpan<uint> tags, bool tryPreserveExistingTagsAndPenalties = true)
        {
        }

        internal void ReplaceTilePostCut(int x, int z, UnsafeSpan<Int3> verts, UnsafeSpan<int> tris, UnsafeSpan<uint> tags, bool tryPreserveExistingTagsAndPenalties = true, bool preservePreCutData = false)
        {
        }

        internal static void CreateNodes(NavmeshTile tile, UnsafeSpan<int> tris, int tileIndex, uint graphIndex, UnsafeSpan<uint> tags, bool initializeNodes, AstarPath astar, uint initialPenalty, bool tryPreserveExistingTagsAndPenalties)
        {
        }

        public NavmeshBase()
        {
        }

        /// <summary>
        /// Returns if there is an obstacle between start and end on the graph.
        /// This is not the same as Physics.Linecast, this function traverses the \b graph and looks for collisions instead of checking for collider intersection.
        ///
        /// <code>
        /// var gg = AstarPath.active.data.gridGraph;
        /// bool anyObstaclesInTheWay = gg.Linecast(transform.position, enemy.position);
        /// </code>
        ///
        /// [Open online documentation to see images]
        /// </summary>
        /// <param name="start">Point to linecast from. In world space.</param>
        /// <param name="end">Point to linecast to. In world space.</param>
        public bool Linecast(Vector3 start, Vector3 end)
        {
            return default;
        }

        /// <summary>
        /// Returns if there is an obstacle between start and end on the graph.
        ///
        /// This is not the same as Physics.Linecast, this function traverses the \b graph and looks for collisions instead of checking for collider intersection.
        ///
        /// <code>
        /// var graph = AstarPath.active.data.recastGraph;
        /// var start = transform.position;
        /// var end = start + Vector3.forward * 10;
        /// var trace = new List<GraphNode>();
        /// if (graph.Linecast(start, end, out GraphHitInfo hit, trace, null)) {
        ///     Debug.Log("Linecast traversed " + trace.Count + " nodes before hitting an obstacle");
        ///     Debug.DrawLine(start, hit.point, Color.red);
        ///     Debug.DrawLine(hit.point, end, Color.blue);
        /// } else {
        ///     Debug.Log("Linecast traversed " + trace.Count + " nodes");
        ///     Debug.DrawLine(start, end, Color.green);
        /// }
        /// </code>
        ///
        /// [Open online documentation to see images]
        /// </summary>
        /// <param name="start">Point to linecast from. In world space.</param>
        /// <param name="end">Point to linecast to. In world space.</param>
        /// <param name="hit">Contains info on what was hit, see GraphHitInfo.</param>
        /// <param name="hint">If you know which node the start point is on, you can pass it here to save a GetNearest call, resulting in a minor performance boost. Otherwise, pass null. The start point will be clamped to the surface of this node.</param>
        public bool Linecast(Vector3 start, Vector3 end, GraphNode hint, out GraphHitInfo hit)
        {
            hit = default(GraphHitInfo);
            return default;
        }

        /// <summary>
        /// Returns if there is an obstacle between start and end on the graph.
        ///
        /// This is not the same as Physics.Linecast, this function traverses the \b graph and looks for collisions instead of checking for collider intersection.
        ///
        /// <code>
        /// var gg = AstarPath.active.data.gridGraph;
        /// bool anyObstaclesInTheWay = gg.Linecast(transform.position, enemy.position);
        /// </code>
        ///
        /// [Open online documentation to see images]
        /// </summary>
        /// <param name="start">Point to linecast from. In world space.</param>
        /// <param name="end">Point to linecast to. In world space.</param>
        /// <param name="hint">If you know which node the start point is on, you can pass it here to save a GetNearest call, resulting in a minor performance boost. Otherwise, pass null. The start point will be clamped to the surface of this node.</param>
        public bool Linecast(Vector3 start, Vector3 end, GraphNode hint)
        {
            return default;
        }

        /// <summary>
        /// Returns if there is an obstacle between start and end on the graph.
        ///
        /// This is not the same as Physics.Linecast, this function traverses the \b graph and looks for collisions instead of checking for collider intersection.
        ///
        /// <code>
        /// var graph = AstarPath.active.data.recastGraph;
        /// var start = transform.position;
        /// var end = start + Vector3.forward * 10;
        /// var trace = new List<GraphNode>();
        /// if (graph.Linecast(start, end, out GraphHitInfo hit, trace, null)) {
        ///     Debug.Log("Linecast traversed " + trace.Count + " nodes before hitting an obstacle");
        ///     Debug.DrawLine(start, hit.point, Color.red);
        ///     Debug.DrawLine(hit.point, end, Color.blue);
        /// } else {
        ///     Debug.Log("Linecast traversed " + trace.Count + " nodes");
        ///     Debug.DrawLine(start, end, Color.green);
        /// }
        /// </code>
        ///
        /// [Open online documentation to see images]
        /// </summary>
        /// <param name="start">Point to linecast from. In world space.</param>
        /// <param name="end">Point to linecast to. In world space.</param>
        /// <param name="hit">Contains info on what was hit, see GraphHitInfo.</param>
        /// <param name="trace">If a list is passed, then it will be filled with all nodes the linecast traverses.</param>
        /// <param name="hint">If you know which node the start point is on, you can pass it here to save a GetNearest call, resulting in a minor performance boost. Otherwise, pass null. The start point will be clamped to the surface of this node.</param>
        public bool Linecast(Vector3 start, Vector3 end, GraphNode hint, out GraphHitInfo hit, List<GraphNode> trace)
        {
            hit = default(GraphHitInfo);
            return default;
        }

        /// <summary>
        /// Returns if there is an obstacle between start and end on the graph.
        ///
        /// This is not the same as Physics.Linecast, this function traverses the \b graph and looks for collisions instead of checking for collider intersection.
        ///
        /// <code>
        /// var graph = AstarPath.active.data.recastGraph;
        /// var start = transform.position;
        /// var end = start + Vector3.forward * 10;
        /// var trace = new List<GraphNode>();
        /// if (graph.Linecast(start, end, out GraphHitInfo hit, trace, null)) {
        ///     Debug.Log("Linecast traversed " + trace.Count + " nodes before hitting an obstacle");
        ///     Debug.DrawLine(start, hit.point, Color.red);
        ///     Debug.DrawLine(hit.point, end, Color.blue);
        /// } else {
        ///     Debug.Log("Linecast traversed " + trace.Count + " nodes");
        ///     Debug.DrawLine(start, end, Color.green);
        /// }
        /// </code>
        ///
        /// [Open online documentation to see images]
        /// </summary>
        /// <param name="start">Point to linecast from. In world space.</param>
        /// <param name="end">Point to linecast to. In world space.</param>
        /// <param name="hit">Contains info on what was hit, see GraphHitInfo.</param>
        /// <param name="trace">If a list is passed, then it will be filled with all nodes the linecast traverses.</param>
        /// <param name="filter">If not null then the delegate will be called for each node and if it returns false the node will be treated as unwalkable and a hit will be returned.
        ///               Note that unwalkable nodes are always treated as unwalkable regardless of what this filter returns.</param>
        public bool Linecast(Vector3 start, Vector3 end, out GraphHitInfo hit, List<GraphNode> trace, System.Func<GraphNode, bool> filter)
        {
            hit = default(GraphHitInfo);
            return default;
        }

        /// <summary>
        /// Returns if there is an obstacle between start and end on the graph.
        ///
        /// This is not the same as Physics.Linecast, this function traverses the \b graph and looks for collisions instead of checking for collider intersection.
        ///
        /// <code>
        /// var graph = AstarPath.active.data.recastGraph;
        /// var start = transform.position;
        /// var end = start + Vector3.forward * 10;
        /// var trace = new List<GraphNode>();
        /// if (graph.Linecast(start, end, out GraphHitInfo hit, trace, null)) {
        ///     Debug.Log("Linecast traversed " + trace.Count + " nodes before hitting an obstacle");
        ///     Debug.DrawLine(start, hit.point, Color.red);
        ///     Debug.DrawLine(hit.point, end, Color.blue);
        /// } else {
        ///     Debug.Log("Linecast traversed " + trace.Count + " nodes");
        ///     Debug.DrawLine(start, end, Color.green);
        /// }
        /// </code>
        ///
        /// [Open online documentation to see images]
        /// </summary>
        /// <param name="start">Point to linecast from. In world space.</param>
        /// <param name="end">Point to linecast to. In world space.</param>
        /// <param name="hit">Contains info on what was hit, see GraphHitInfo.</param>
        /// <param name="trace">If a list is passed, then it will be filled with all nodes the linecast traverses.</param>
        /// <param name="filter">If not null then the delegate will be called for each node and if it returns false the node will be treated as unwalkable and a hit will be returned.
        ///               Note that unwalkable nodes are always treated as unwalkable regardless of what this filter returns.</param>
        /// <param name="hint">If you know which node the start point is on, you can pass it here to save a GetNearest call, resulting in a minor performance boost. Otherwise, pass null. The start point will be clamped to the surface of this node.</param>
        public bool Linecast(Vector3 start, Vector3 end, GraphNode hint, out GraphHitInfo hit, List<GraphNode> trace, System.Func<GraphNode, bool> filter)
        {
            hit = default(GraphHitInfo);
            return default;
        }


        /// <summary>
        /// Returns if there is an obstacle between start and end on the graph.
        ///
        /// This is not the same as Physics.Linecast, this function traverses the \b graph and looks for collisions instead of checking for collider intersection.
        ///
        /// <code>
        /// var graph = AstarPath.active.data.recastGraph;
        /// var start = transform.position;
        /// var end = start + Vector3.forward * 10;
        /// var trace = new List<GraphNode>();
        /// if (graph.Linecast(start, end, out GraphHitInfo hit, trace, null)) {
        ///     Debug.Log("Linecast traversed " + trace.Count + " nodes before hitting an obstacle");
        ///     Debug.DrawLine(start, hit.point, Color.red);
        ///     Debug.DrawLine(hit.point, end, Color.blue);
        /// } else {
        ///     Debug.Log("Linecast traversed " + trace.Count + " nodes");
        ///     Debug.DrawLine(start, end, Color.green);
        /// }
        /// </code>
        ///
        /// [Open online documentation to see images]
        /// </summary>
        /// <param name="graph">The graph to perform the search on.</param>
        /// <param name="start">Point to start from. In world space.</param>
        /// <param name="end">Point to linecast to. In world space.</param>
        /// <param name="hit">Contains info on what was hit, see GraphHitInfo.</param>
        /// <param name="hint">If you know which node the start point is on, you can pass it here to save a GetNearest call, resulting in a minor performance boost. Otherwise, pass null. The start point will be clamped to the surface of this node.</param>
        public static bool Linecast(NavmeshBase graph, Vector3 start, Vector3 end, GraphNode hint, out GraphHitInfo hit)
        {
            hit = default(GraphHitInfo);
            return default;
        }

        /// <summary>Cached <see cref="Pathfinding.NNConstraint.None"/> with distanceXZ=true to reduce allocations</summary>
        static readonly NNConstraint NNConstraintNoneXZ = new NNConstraint
        {
            constrainWalkability = false,
            constrainArea = false,
            constrainTags = false,
            constrainDistance = false,
            graphMask = -1,
        };

        /// <summary>Used to optimize linecasts by precomputing some values</summary>
        static readonly byte[] LinecastShapeEdgeLookup;

        static NavmeshBase()
        {
        }

        /// <summary>
        /// Returns if there is an obstacle between origin and end on the graph.
        ///
        /// This is not the same as Physics.Linecast, this function traverses the \b graph and looks for collisions instead of checking for collider intersections.
        ///
        /// Note: This method only makes sense for graphs in which there is a definite 'up' direction. For example it does not make sense for e.g spherical graphs,
        /// navmeshes in which characters can walk on walls/ceilings or other curved worlds. If you try to use this method on such navmeshes it may output nonsense.
        ///
        /// <code>
        /// var graph = AstarPath.active.data.recastGraph;
        /// var start = transform.position;
        /// var end = start + Vector3.forward * 10;
        /// var trace = new List<GraphNode>();
        /// if (graph.Linecast(start, end, out GraphHitInfo hit, trace, null)) {
        ///     Debug.Log("Linecast traversed " + trace.Count + " nodes before hitting an obstacle");
        ///     Debug.DrawLine(start, hit.point, Color.red);
        ///     Debug.DrawLine(hit.point, end, Color.blue);
        /// } else {
        ///     Debug.Log("Linecast traversed " + trace.Count + " nodes");
        ///     Debug.DrawLine(start, end, Color.green);
        /// }
        /// </code>
        ///
        /// [Open online documentation to see images]
        /// </summary>
        /// <param name="graph">The graph to perform the search on</param>
        /// <param name="origin">Point to start from. This point should be on the navmesh. It will be snapped to the closest point on the navmesh otherwise.</param>
        /// <param name="end">Point to linecast to</param>
        /// <param name="hit">Contains info on what was hit, see GraphHitInfo</param>
        /// <param name="hint">If you already know the node which contains the origin point, you may pass it here for slighly improved performance. If null, a search for the closest node will be done.</param>
        /// <param name="trace">If a list is passed, then it will be filled with all nodes along the line up until it hits an obstacle or reaches the end.</param>
        /// <param name="filter">If not null then the delegate will be called for each node and if it returns false the node will be treated as unwalkable and a hit will be returned.
        ///               Note that unwalkable nodes are always treated as unwalkable regardless of what this filter returns.</param>
        public static bool Linecast(NavmeshBase graph, Vector3 origin, Vector3 end, GraphNode hint, out GraphHitInfo hit, List<GraphNode> trace, System.Func<GraphNode, bool> filter = null)
        {
            hit = default(GraphHitInfo);
            return default;
        }

        /// <summary>Start at node, then walk around the given vertex and see if targetNode is reachable by doing this.</summary>
        /// <param name="node">The node to start from</param>
        /// <param name="targetNode">The node to check if it is reachable</param>
        /// <param name="vertexInGraphSpace">The vertex to walk around</param>
        /// <param name="oppositeDirection">If true, walk in the opposite direction around the vertex
        /// \return True if the target node is reachable</param>
        static bool FindNodeAroundVertex(TriangleMeshNode node, TriangleMeshNode targetNode, Int3 vertexInGraphSpace, bool oppositeDirection)
        {
            return default;
        }

        public override void OnDrawGizmos (DrawingData gizmos, bool drawNodes, RedrawScope redrawScope)
        {
        }

        /// <summary>Creates a mesh of the surfaces of the navmesh for use in OnDrawGizmos in the editor</summary>
        void CreateNavmeshSurfaceVisualization(NavmeshTile[] tiles, int startTile, int endTile, GraphGizmoHelper helper)
        {
        }

        /// <summary>Creates an outline of the navmesh for use in OnDrawGizmos in the editor</summary>
        static void CreateNavmeshOutlineVisualization(NavmeshTile[] tiles, int startTile, int endTile, GraphGizmoHelper helper)
        {
        }

        /// <summary>
        /// Serializes Node Info.
        /// Should serialize:
        /// - Base
        ///    - Node Flags
        ///    - Node Penalties
        ///    - Node
        /// - Node Positions (if applicable)
        /// - Any other information necessary to load the graph in-game
        /// All settings marked with json attributes (e.g JsonMember) have already been
        /// saved as graph settings and do not need to be handled here.
        ///
        /// It is not necessary for this implementation to be forward or backwards compatible.
        /// </summary>
        protected override void SerializeExtraInfo (GraphSerializationContext ctx) {
        }

        protected override void DeserializeExtraInfo(GraphSerializationContext ctx)
        {
        }

        protected override void PostDeserialization (GraphSerializationContext ctx) {
        }
    }
}
