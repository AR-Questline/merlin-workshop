using System.Collections.Generic;
using Math = System.Math;
using UnityEngine;
using System.Linq;
using UnityEngine.Profiling;


namespace Pathfinding {
	using Pathfinding.Serialization;
	using Pathfinding.Util;
	using Unity.Collections;
	using Unity.Jobs;
	using Unity.Mathematics;
	using Pathfinding.Jobs;
	using Pathfinding.Graphs.Grid.Jobs;
	using Pathfinding.Collections;
	using Pathfinding.Drawing;
	using Pathfinding.Graphs.Grid;
	using Pathfinding.Graphs.Grid.Rules;
	using Pathfinding.Pooling;
	using UnityEngine.Assertions;

	/// <summary>
	/// Generates a grid of nodes.
	/// [Open online documentation to see images]
	/// The GridGraph does exactly what the name implies, generates nodes in a grid pattern.
	///
	/// Grid graphs are excellent for when you already have a grid-based world. But they also work well for free-form worlds.
	///
	/// See: get-started-grid (view in online documentation for working links)
	/// See: graphTypes (view in online documentation for working links)
	///
	/// \section gridgraph-features Features
	/// - Throw any scene at it, and with minimal configurations you can get a good graph from it.
	/// - Predictable pattern.
	/// - Grid graphs work well with penalties and tags.
	/// - You can update parts of the graph during runtime.
	/// - Graph updates are fast.
	/// - Scanning the graph is comparatively fast.
	/// - Supports linecasting.
	/// - Supports the funnel modifier.
	/// - Supports both 2D and 3D physics.
	/// - Supports isometric and hexagonal node layouts.
	/// - Can apply penalty and walkability values from a supplied image.
	/// - Perfect for terrains since it can make nodes walkable or unwalkable depending on the slope.
	/// - Only supports a single layer, but you can use a <see cref="LayerGridGraph"/> if you need more layers.
	///
	/// \section gridgraph-inspector Inspector
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
	/// <i>Collision testing</i>
	/// \inspectorField{Collider type, collision.type}
	/// \inspectorField{Diameter, collision.diameter}
	/// \inspectorField{Height/length, collision.height}
	/// \inspectorField{Offset, collision.collisionOffset}
	/// \inspectorField{Obstacle layer mask, collision.mask}
	/// \inspectorField{Preview, GridGraphEditor.collisionPreviewOpen}
	///
	/// <i>Height testing</i>
	/// \inspectorField{Ray length, collision.fromHeight}
	/// \inspectorField{Mask, collision.heightMask}
	/// \inspectorField{Thick raycast, collision.thickRaycast}
	/// \inspectorField{Unwalkable when no ground, collision.unwalkableWhenNoGround}
	///
	/// <i>Rules</i>
	/// Take a look at grid-rules (view in online documentation for working links) for a list of available rules.
	///
	/// <i>Other settings</i>
	/// \inspectorField{Show surface, showMeshSurface}
	/// \inspectorField{Show outline, showMeshOutline}
	/// \inspectorField{Show connections, showNodeConnections}
	/// \inspectorField{Initial penalty, NavGraph.initialPenalty}
	///
	/// \section gridgraph-updating Updating the graph during runtime
	/// Any graph which implements the IUpdatableGraph interface can be updated during runtime.
	/// For grid graphs this is a great feature since you can update only a small part of the grid without causing any lag like a complete rescan would.
	///
	/// If you for example just have instantiated an obstacle in the scene and you want to update the grid where that obstacle was instantiated, you can do this:
	///
	/// <code> AstarPath.active.UpdateGraphs (obstacle.collider.bounds); </code>
	/// Where obstacle is the GameObject you just instantiated.
	///
	/// As you can see, the UpdateGraphs function takes a Bounds parameter and it will send an update call to all updateable graphs.
	///
	/// A grid graph will assume anything could have changed inside that bounding box, and recalculate all nodes that could possibly be affected.
	/// Thus it may end up updating a few more nodes than just those covered by the bounding box.
	///
	/// See: graph-updates (view in online documentation for working links) for more info about updating graphs during runtime
	///
	/// \section gridgraph-hexagonal Hexagonal graphs
	/// The graph can be configured to work like a hexagon graph with some simple settings. The grid graph has a Shape dropdown.
	/// If you set it to 'Hexagonal' the graph will behave as a hexagon graph.
	/// Often you may want to rotate the graph +45 or -45 degrees.
	/// [Open online documentation to see images]
	///
	/// Note: Snapping to the closest node is not exactly as you would expect in a real hexagon graph,
	/// but it is close enough that you will likely not notice.
	///
	/// \section gridgraph-configure-code Configure using code
	///
	/// A grid graph can be added and configured completely at runtime via code.
	///
	/// <code>
	/// // This holds all graph data
	/// AstarData data = AstarPath.active.data;
	///
	/// // This creates a Grid Graph
	/// GridGraph gg = data.AddGraph(typeof(GridGraph)) as GridGraph;
	///
	/// // Setup a grid graph with some values
	/// int width = 50;
	/// int depth = 50;
	/// float nodeSize = 1;
	///
	/// gg.center = new Vector3(10, 0, 0);
	///
	/// // Updates internal size from the above values
	/// gg.SetDimensions(width, depth, nodeSize);
	///
	/// // Scans all graphs
	/// AstarPath.active.Scan();
	/// </code>
	///
	/// See: runtime-graphs (view in online documentation for working links)
	///
	/// \section gridgraph-trees Tree colliders
	/// It seems that Unity will only generate tree colliders at runtime when the game is started.
	/// For this reason, the grid graph will not pick up tree colliders when outside of play mode
	/// but it will pick them up once the game starts. If it still does not pick them up
	/// make sure that the trees actually have colliders attached to them and that the tree prefabs are
	/// in the correct layer (the layer should be included in the 'Collision Testing' mask).
	///
	/// See: <see cref="GraphCollision"/> for documentation on the 'Height Testing' and 'Collision Testing' sections
	/// of the grid graph settings.
	/// See: <see cref="LayerGridGraph"/>
	/// </summary>
	[JsonOptIn]
	[Pathfinding.Util.Preserve]
	public class GridGraph : NavGraph, IUpdatableGraph, ITransformedGraph
		, IRaycastableGraph {
		protected override void DisposeUnmanagedData () {
        }

        protected override void DestroyAllNodes () {
        }


        /// <summary>
        /// Number of layers in the graph.
        /// For grid graphs this is always 1, for layered grid graphs it can be higher.
        /// The nodes array has the size width*depth*layerCount.
        /// </summary>
        public virtual int LayerCount {
			get => 1;
			protected set {
				if (value != 1) throw new System.NotSupportedException("Grid graphs cannot have multiple layers");
			}
		}

		public virtual int MaxLayers => 1;

		public override int CountNodes () {
            return default;
        }

        public override void GetNodes(System.Action<GraphNode> action)
        {
        }

        /// <summary>
        /// Determines the layout of the grid graph inspector in the Unity Editor.
        ///
        /// A grid graph can be set up as a normal grid, isometric grid or hexagonal grid.
        /// Each of these modes use a slightly different inspector layout.
        /// When changing the shape in the inspector, it will automatically set other relevant fields
        /// to appropriate values. For example, when setting the shape to hexagonal it will automatically set
        /// the <see cref="neighbours"/> field to Six.
        ///
        /// This field is only used in the editor, it has no effect on the rest of the game whatsoever.
        ///
        /// If you want to change the grid shape like in the inspector you can use the <see cref="SetGridShape"/> method.
        /// </summary>
        [JsonMember]
		public InspectorGridMode inspectorGridMode = InspectorGridMode.Grid;

		/// <summary>
		/// Determines how the size of each hexagon is set in the inspector.
		/// For hexagons the normal nodeSize field doesn't really correspond to anything specific on the hexagon's geometry, so this enum is used to give the user the opportunity to adjust more concrete dimensions of the hexagons
		/// without having to pull out a calculator to calculate all the square roots and complicated conversion factors.
		///
		/// This field is only used in the graph inspector, the <see cref="nodeSize"/> field will always use the same internal units.
		/// If you want to set the node size through code then you can use <see cref="ConvertHexagonSizeToNodeSize"/>.
		///
		/// [Open online documentation to see images]
		///
		/// See: <see cref="InspectorGridHexagonNodeSize"/>
		/// See: <see cref="ConvertHexagonSizeToNodeSize"/>
		/// See: <see cref="ConvertNodeSizeToHexagonSize"/>
		/// </summary>
		[JsonMember]
		public InspectorGridHexagonNodeSize inspectorHexagonSizeMode = InspectorGridHexagonNodeSize.Width;

		/// <summary>
		/// Width of the grid in nodes.
		///
		/// Grid graphs are typically anywhere from 10-500 nodes wide. But it can go up to 1024 nodes wide by default.
		/// Consider using a recast graph instead, if you find yourself needing a very high resolution grid.
		///
		/// This value will be clamped to at most 1024 unless ASTAR_LARGER_GRIDS has been enabled in the A* Inspector -> Optimizations tab.
		///
		/// See: <see cref="depth"/>
		/// See: SetDimensions
		/// </summary>
		public int width;

		/// <summary>
		/// Depth (height) of the grid in nodes.
		///
		/// Grid graphs are typically anywhere from 10-500 nodes wide. But it can go up to 1024 nodes wide by default.
		/// Consider using a recast graph instead, if you find yourself needing a very high resolution grid.
		///
		/// This value will be clamped to at most 1024 unless ASTAR_LARGER_GRIDS has been enabled in the A* Inspector -> Optimizations tab.
		///
		/// See: <see cref="width"/>
		/// See: SetDimensions
		/// </summary>
		public int depth;

		/// <summary>
		/// Scaling of the graph along the X axis.
		/// This should be used if you want different scales on the X and Y axis of the grid
		///
		/// This option is only visible in the inspector if the graph shape is set to isometric or advanced.
		/// </summary>
		[JsonMember]
		public float aspectRatio = 1F;

		/// <summary>
		/// Angle in degrees to use for the isometric projection.
		/// If you are making a 2D isometric game, you may want to use this parameter to adjust the layout of the graph to match your game.
		/// This will essentially scale the graph along one of its diagonals to produce something like this:
		///
		/// A perspective view of an isometric graph.
		/// [Open online documentation to see images]
		///
		/// A top down view of an isometric graph. Note that the graph is entirely 2D, there is no perspective in this image.
		/// [Open online documentation to see images]
		///
		/// For commonly used values see <see cref="StandardIsometricAngle"/> and <see cref="StandardDimetricAngle"/>.
		///
		/// Usually the angle that you want to use is either 30 degrees (alternatively 90-30 = 60 degrees) or atan(1/sqrt(2)) which is approximately 35.264 degrees (alternatively 90 - 35.264 = 54.736 degrees).
		/// You might also want to rotate the graph plus or minus 45 degrees around the Y axis to get the oritientation required for your game.
		///
		/// You can read more about it on the wikipedia page linked below.
		///
		/// See: http://en.wikipedia.org/wiki/Isometric_projection
		/// See: https://en.wikipedia.org/wiki/Isometric_graphics_in_video_games_and_pixel_art
		/// See: rotation
		///
		/// This option is only visible in the inspector if the graph shape is set to isometric or advanced.
		/// </summary>
		[JsonMember]
		public float isometricAngle;

		/// <summary>Commonly used value for <see cref="isometricAngle"/></summary>
		public static readonly float StandardIsometricAngle = 90-Mathf.Atan(1/Mathf.Sqrt(2))*Mathf.Rad2Deg;

		/// <summary>Commonly used value for <see cref="isometricAngle"/></summary>
		public static readonly float StandardDimetricAngle = Mathf.Acos(1/2f)*Mathf.Rad2Deg;

		/// <summary>
		/// If true, all edge costs will be set to the same value.
		/// If false, diagonals will cost more.
		/// This is useful for a hexagon graph where the diagonals are actually the same length as the
		/// normal edges (since the graph has been skewed)
		///
		/// If the graph is set to hexagonal in the inspector, this will be automatically set to true.
		/// </summary>
		[JsonMember]
		public bool uniformEdgeCosts;

		/// <summary>
		/// Rotation of the grid in degrees.
		///
		/// The nodes are laid out along the X and Z axes of the rotation.
		///
		/// For a 2D game, the rotation will typically be set to (-90, 270, 90).
		/// If the graph is aligned with the XY plane, the inspector will automatically switch to 2D mode.
		///
		/// See: <see cref="is2D"/>
		/// </summary>
		[JsonMember]
		public Vector3 rotation;

		/// <summary>
		/// Center point of the grid in world space.
		///
		/// The graph can be positioned anywhere in the world.
		///
		/// See: <see cref="RelocateNodes(Vector3,Quaternion,float,float,float)"/>
		/// </summary>
		[JsonMember]
		public Vector3 center;

		/// <summary>Size of the grid. Can be negative or smaller than <see cref="nodeSize"/></summary>
		[JsonMember]
		public Vector2 unclampedSize = new Vector2(10, 10);

		/// <summary>
		/// Size of one node in world units.
		///
		/// For a grid layout, this is the length of the sides of the grid squares.
		///
		/// For a hexagonal layout, this value does not correspond to any specific dimension of the hexagon.
		/// Instead you can convert it to a dimension on a hexagon using <see cref="ConvertNodeSizeToHexagonSize"/>.
		///
		/// See: <see cref="SetDimensions"/>
		/// See: <see cref="SetGridShape"/>
		/// </summary>
		[JsonMember]
		public float nodeSize = 1;

		/// <summary>Settings on how to check for walkability and height</summary>
		[JsonMember]
		public GraphCollision collision = new GraphCollision();

		/// <summary>
		/// The max y coordinate difference between two nodes to enable a connection.
		/// Set to 0 to ignore the value.
		///
		/// This affects for example how the graph is generated around ledges and stairs.
		///
		/// See: <see cref="maxStepUsesSlope"/>
		/// Version: Was previously called maxClimb
		/// </summary>
		[JsonMember]
		public float maxStepHeight = 0.4F;

		/// <summary>
		/// The max y coordinate difference between two nodes to enable a connection.
		/// Deprecated: This field has been renamed to <see cref="maxStepHeight"/>
		/// </summary>
		[System.Obsolete("This field has been renamed to maxStepHeight")]
		public float maxClimb {
			get {
				return maxStepHeight;
			}
			set {
				maxStepHeight = value;
			}
		}

		/// <summary>
		/// Take the slope into account for <see cref="maxStepHeight"/>.
		///
		/// When this is enabled the normals of the terrain will be used to make more accurate estimates of how large the steps are between adjacent nodes.
		///
		/// When this is disabled then calculated step between two nodes is their y coordinate difference. This may be inaccurate, especially at the start of steep slopes.
		///
		/// [Open online documentation to see images]
		///
		/// In the image below you can see an example of what happens near a ramp.
		/// In the topmost image the ramp is not connected with the rest of the graph which is obviously not what we want.
		/// In the middle image an attempt has been made to raise the max step height while keeping <see cref="maxStepUsesSlope"/> disabled. However this causes too many connections to be added.
		/// The agent should not be able to go up the ramp from the side.
		/// Finally in the bottommost image the <see cref="maxStepHeight"/> has been restored to the original value but <see cref="maxStepUsesSlope"/> has been enabled. This configuration handles the ramp in a much smarter way.
		/// Note that all the values in the image are just example values, they may be different for your scene.
		/// [Open online documentation to see images]
		///
		/// See: <see cref="maxStepHeight"/>
		/// </summary>
		[JsonMember]
		public bool maxStepUsesSlope = true;

		/// <summary>The max slope in degrees for a node to be walkable.</summary>
		[JsonMember]
		public float maxSlope = 90;

		/// <summary>
		/// Use heigh raycasting normal for max slope calculation.
		/// True if <see cref="maxSlope"/> is less than 90 degrees.
		/// </summary>
		protected bool useRaycastNormal { get { return Math.Abs(90-maxSlope) > float.Epsilon; } }

		/// <summary>
		/// Number of times to erode the graph.
		///
		/// The graph can be eroded to add extra margin to obstacles.
		/// It is very convenient if your graph contains ledges, and where the walkable nodes without erosion are too close to the edge.
		///
		/// Below is an image showing a graph with 0, 1 and 2 erosion iterations:
		/// [Open online documentation to see images]
		///
		/// Note: A high number of erosion iterations can slow down graph updates during runtime.
		/// This is because the region that is updated needs to be expanded by the erosion iterations times two to account for possible changes in the border nodes.
		///
		/// See: erosionUseTags
		/// </summary>
		[JsonMember]
		public int erodeIterations;

		/// <summary>
		/// Use tags instead of walkability for erosion.
		/// Tags will be used for erosion instead of marking nodes as unwalkable. The nodes will be marked with tags in an increasing order starting with the tag <see cref="erosionFirstTag"/>.
		/// Debug with the Tags mode to see the effect. With this enabled you can in effect set how close different AIs are allowed to get to walls using the Valid Tags field on the Seeker component.
		/// [Open online documentation to see images]
		/// [Open online documentation to see images]
		/// See: erosionFirstTag
		/// </summary>
		[JsonMember]
		public bool erosionUseTags;

		/// <summary>
		/// Tag to start from when using tags for erosion.
		/// See: <see cref="erosionUseTags"/>
		/// See: <see cref="erodeIterations"/>
		/// </summary>
		[JsonMember]
		public int erosionFirstTag = 1;

		/// <summary>
		/// Bitmask for which tags can be overwritten by erosion tags.
		///
		/// When <see cref="erosionUseTags"/> is enabled, nodes near unwalkable nodes will be marked with tags.
		/// However, if these nodes already have tags, you may want the custom tag to take precedence.
		/// This mask controls which tags are allowed to be replaced by the new erosion tags.
		///
		/// In the image below, erosion has applied tags which have overwritten both the base tag (tag 0) and the custom tag set on the nodes (shown in red).
		/// [Open online documentation to see images]
		///
		/// In the image below, erosion has applied tags, but it was not allowed to overwrite the custom tag set on the nodes (shown in red).
		/// [Open online documentation to see images]
		///
		/// See: <see cref="erosionUseTags"/>
		/// See: <see cref="erodeIterations"/>
		/// See: This field is a bit mask. See: bitmasks (view in online documentation for working links)
		/// </summary>
		[JsonMember]
		public int erosionTagsPrecedenceMask = -1;

		/// <summary>
		/// Number of neighbours for each node.
		/// Either four, six, eight connections per node.
		///
		/// Six connections is primarily for hexagonal graphs.
		/// </summary>
		[JsonMember]
		public NumNeighbours neighbours = NumNeighbours.Eight;

		/// <summary>
		/// If disabled, will not cut corners on obstacles.
		/// If this is true, and <see cref="neighbours"/> is set to Eight, obstacle corners are allowed to be cut by a connection.
		///
		/// [Open online documentation to see images]
		/// </summary>
		[JsonMember]
		public bool cutCorners = true;

		/// <summary>
		/// Offset for the position when calculating penalty.
		/// Deprecated: Use the RuleElevationPenalty class instead
		/// See: penaltyPosition
		/// </summary>
		[JsonMember]
		[System.Obsolete("Use the RuleElevationPenalty class instead")]
		public float penaltyPositionOffset;

		/// <summary>
		/// Use position (y-coordinate) to calculate penalty.
		/// Deprecated: Use the RuleElevationPenalty class instead
		/// </summary>
		[JsonMember]
		[System.Obsolete("Use the RuleElevationPenalty class instead")]
		public bool penaltyPosition;

		/// <summary>
		/// Scale factor for penalty when calculating from position.
		/// Deprecated: Use the <see cref="RuleElevationPenalty"/> class instead
		/// See: penaltyPosition
		/// </summary>
		[JsonMember]
		[System.Obsolete("Use the RuleElevationPenalty class instead")]
		public float penaltyPositionFactor = 1F;

		/// <summary>Deprecated: Use the <see cref="RuleAnglePenalty"/> class instead</summary>
		[JsonMember]
		[System.Obsolete("Use the RuleAnglePenalty class instead")]
		public bool penaltyAngle;

		/// <summary>
		/// How much penalty is applied depending on the slope of the terrain.
		/// At a 90 degree slope (not that exactly 90 degree slopes can occur, but almost 90 degree), this penalty is applied.
		/// At a 45 degree slope, half of this is applied and so on.
		/// Note that you may require very large values, a value of 1000 is equivalent to the cost of moving 1 world unit.
		///
		/// Deprecated: Use the <see cref="RuleAnglePenalty"/> class instead
		/// </summary>
		[JsonMember]
		[System.Obsolete("Use the RuleAnglePenalty class instead")]
		public float penaltyAngleFactor = 100F;

		/// <summary>
		/// How much extra to penalize very steep angles.
		///
		/// Deprecated: Use the <see cref="RuleAnglePenalty"/> class instead
		/// </summary>
		[JsonMember]
		[System.Obsolete("Use the RuleAnglePenalty class instead")]
		public float penaltyAnglePower = 1;

		/// <summary>
		/// Additional rules to use when scanning the grid graph.
		///
		/// <code>
		/// // Get the first grid graph in the scene
		/// var gridGraph = AstarPath.active.data.gridGraph;
		///
		/// gridGraph.rules.AddRule(new Pathfinding.Graphs.Grid.Rules.RuleAnglePenalty {
		///     penaltyScale = 10000,
		///     curve = AnimationCurve.Linear(0, 0, 90, 1),
		/// });
		/// </code>
		///
		/// See: <see cref="GridGraphRules"/>
		/// See: <see cref="GridGraphRule"/>
		/// </summary>
		[JsonMember]
		public GridGraphRules rules = new GridGraphRules();

		/// <summary>Show an outline of the grid nodes in the Unity Editor</summary>
		[JsonMember]
		public bool showMeshOutline = true;

		/// <summary>Show the connections between the grid nodes in the Unity Editor</summary>
		[JsonMember]
		public bool showNodeConnections;

		/// <summary>Show the surface of the graph. Each node will be drawn as a square (unless e.g hexagon graph mode has been enabled).</summary>
		[JsonMember]
		public bool showMeshSurface = true;

		/// <summary>
		/// Holds settings for using a texture as source for a grid graph.
		/// Texure data can be used for fine grained control over how the graph will look.
		/// It can be used for positioning, penalty and walkability control.
		/// Below is a screenshot of a grid graph with a penalty map applied.
		/// It has the effect of the AI taking the longer path along the green (low penalty) areas.
		/// [Open online documentation to see images]
		/// Color data is got as 0...255 values.
		///
		/// Warning: Can only be used with Unity 3.4 and up
		///
		/// Deprecated: Use the RuleTexture class instead
		/// </summary>
		[JsonMember]
		[System.Obsolete("Use the RuleTexture class instead")]
		public TextureData textureData = new TextureData();

		/// <summary>
		/// Size of the grid. Will always be positive and larger than <see cref="nodeSize"/>.
		/// See: <see cref="UpdateTransform"/>
		/// </summary>
		public Vector2 size { get; protected set; }

		/* End collision and stuff */

		/// <summary>
		/// Index offset to get neighbour nodes. Added to a node's index to get a neighbour node index.
		///
		/// <code>
		///         Z
		///         |
		///         |
		///
		///      6  2  5
		///       \ | /
		/// --  3 - X - 1  ----- X
		///       / | \
		///      7  0  4
		///
		///         |
		///         |
		/// </code>
		/// </summary>
		[System.NonSerialized]
		public readonly int[] neighbourOffsets = new int[8];

		/// <summary>
		/// Costs to neighbour nodes.
		///
		/// See <see cref="neighbourOffsets"/>.
		/// </summary>
		[System.NonSerialized]
		public readonly uint[] neighbourCosts = new uint[8];

		/// <summary>Offsets in the X direction for neighbour nodes. Only 1, 0 or -1</summary>
		public static readonly int[] neighbourXOffsets = { 0, 1, 0, -1, 1, 1, -1, -1 };

		/// <summary>Offsets in the Z direction for neighbour nodes. Only 1, 0 or -1</summary>
		public static readonly int[] neighbourZOffsets = { -1, 0, 1, 0, -1, 1, 1, -1 };

		/// <summary>Which neighbours are going to be used when <see cref="neighbours"/>=6</summary>
		internal static readonly int[] hexagonNeighbourIndices = { 0, 1, 5, 2, 3, 7 };

		/// <summary>Which neighbours are going to be used when <see cref="neighbours"/>=4</summary>
		internal static readonly int[] axisAlignedNeighbourIndices = { 0, 1, 2, 3 };

		/// <summary>Which neighbours are going to be used when <see cref="neighbours"/>=8</summary>
		internal static readonly int[] allNeighbourIndices = { 0, 1, 2, 3, 4, 5, 6, 7 };

		/// <summary>
		/// Neighbour direction indices to use depending on how many neighbours each node should have.
		///
		/// The following illustration shows the direction indices for all 8 neighbours,
		/// <code>
		///         Z
		///         |
		///         |
		///
		///      6  2  5
		///       \ | /
		/// --  3 - X - 1  ----- X
		///       / | \
		///      7  0  4
		///
		///         |
		///         |
		/// </code>
		///
		/// For other neighbour counts, a subset of these will be returned.
		///
		/// These can then be used to index into the <see cref="neighbourOffsets"/>, <see cref="neighbourCosts"/>, <see cref="neighbourXOffsets"/>, and <see cref="neighbourZOffsets"/> arrays.
		///
		/// See: <see cref="GridNodeBase.HasConnectionInDirection"/>
		/// See: <see cref="GridNodeBase.GetNeighbourAlongDirection"/>
		/// </summary>
		public static int[] GetNeighbourDirections (NumNeighbours neighbours) {
            return default;
        }

        /// <summary>
        /// Mask based on hexagonNeighbourIndices.
        /// This indicates which connections (out of the 8 standard ones) should be enabled for hexagonal graphs.
        ///
        /// <code>
        /// int hexagonConnectionMask = 0;
        /// for (int i = 0; i < GridGraph.hexagonNeighbourIndices.Length; i++) hexagonConnectionMask |= 1 << GridGraph.hexagonNeighbourIndices[i];
        /// </code>
        /// </summary>
        internal const int HexagonConnectionMask = 0b010101111;

		/// <summary>
		/// All nodes in this graph.
		/// Nodes are laid out row by row.
		///
		/// The first node has grid coordinates X=0, Z=0, the second one X=1, Z=0
		/// the last one has grid coordinates X=width-1, Z=depth-1.
		///
		/// <code>
		/// var gg = AstarPath.active.data.gridGraph;
		/// int x = 5;
		/// int z = 8;
		/// GridNodeBase node = gg.nodes[z*gg.width + x];
		/// </code>
		///
		/// See: <see cref="GetNode"/>
		/// See: <see cref="GetNodes"/>
		/// </summary>
		public GridNodeBase[] nodes;

		/// <summary>
		/// Internal data for each node.
		///
		/// It also contains some data not stored in the node objects, such as normals for the surface of the graph.
		/// These normals need to be saved when the <see cref="maxStepUsesSlope"/> option is enabled for graph updates to work.
		/// </summary>
		protected GridGraphNodeData nodeData;

		internal ref GridGraphNodeData nodeDataRef => ref nodeData;

		/// <summary>
		/// Determines how the graph transforms graph space to world space.
		/// See: <see cref="UpdateTransform"/>
		/// </summary>
		public GraphTransform transform { get; private set; } = new GraphTransform(Matrix4x4.identity);

		/// <summary>
		/// Delegate which creates and returns a single instance of the node type for this graph.
		/// This may be set in the constructor for graphs inheriting from the GridGraph to change the node type of the graph.
		/// </summary>
		protected System.Func<GridNodeBase> newGridNodeDelegate = () => new GridNode();

		/// <summary>
		/// Get or set if the graph should be in 2D mode.
		///
		/// Note: This is just a convenience property, this property will actually read/modify the <see cref="rotation"/> of the graph. A rotation aligned with the 2D plane is what determines if the graph is 2D or not.
		///
		/// See: You can also set if the graph should use 2D physics using `this.collision.use2D` (<see cref="GraphCollision.use2D"/>).
		/// </summary>
		public bool is2D {
			get {
				return Quaternion.Euler(this.rotation) * Vector3.up == -Vector3.forward;
			}
			set {
				if (value != is2D) {
					this.rotation = value ? new Vector3(this.rotation.y - 90, 270, 90) : new Vector3(0, this.rotation.x + 90, 0);
				}
			}
		}

		public override bool isScanned => nodes != null;

		protected virtual GridNodeBase[] AllocateNodesJob (int size, out JobHandle dependency) {
            dependency = default(JobHandle);
            return default;
        }

        /// <summary>Used for using a texture as a source for a grid graph.</summary>
        public class TextureData {
			public bool enabled;
			public Texture2D source;
			public float[] factors = new float[3];
			public ChannelUse[] channels = new ChannelUse[3];

			Color32[] data;

			/// <summary>Reads texture data</summary>
			public void Initialize () {
            }

            /// <summary>Applies the texture to the node</summary>
            public void Apply(GridNode node, int x, int z)
            {
            }

            /// <summary>Applies a value to the node using the specified ChannelUse</summary>
            void ApplyChannel(GridNode node, int x, int z, int value, ChannelUse channelUse, float factor)
            {
            }

            public enum ChannelUse {
				None,
				Penalty,
				Position,
				WalkablePenalty,
			}
		}

		public override void RelocateNodes (Matrix4x4 deltaMatrix) {
        }

        /// <summary>
        /// Relocate the grid graph using new settings.
        /// This will move all nodes in the graph to new positions which matches the new settings.
        ///
        /// <code>
        /// // Move the graph to the origin, with no rotation, and with a node size of 1.0
        /// var gg = AstarPath.active.data.gridGraph;
        /// gg.RelocateNodes(center: Vector3.zero, rotation: Quaternion.identity, nodeSize: 1.0f);
        /// </code>
        /// </summary>
        public void RelocateNodes (Vector3 center, Quaternion rotation, float nodeSize, float aspectRatio = 1, float isometricAngle = 0) {
        }

        /// <summary>
        /// True if the point is inside the bounding box of this graph.
        ///
        /// This method may be able to use a tighter (non-axis aligned) bounding box than using the one returned by <see cref="bounds"/>.
        ///
        /// For a graph that uses 2D physics, or if height testing is disabled, then the graph is treated as infinitely tall.
        /// Otherwise, the height of the graph is determined by <see cref="GraphCollision.fromHeight"/>.
        ///
        /// Note: For an unscanned graph, this will always return false.
        /// </summary>
        public override bool IsInsideBounds(Vector3 point)
        {
            return default;
        }

        /// <summary>
        /// World bounding box for the graph.
        ///
        /// This always contains the whole graph.
        ///
        /// Note: Since this is an axis-aligned bounding box, it may not be particularly tight if the graph is significantly rotated.
        /// </summary>
        public override Bounds bounds => transform.Transform(new Bounds(new Vector3(width * 0.5f, collision.fromHeight * 0.5f, depth * 0.5f), new Vector3(width, collision.fromHeight, depth)));

        /// <summary>
        /// Transform a point in graph space to world space.
        /// This will give you the node position for the node at the given x and z coordinate
        /// if it is at the specified height above the base of the graph.
        /// </summary>
        public Int3 GraphPointToWorld(int x, int z, float height)
        {
            return default;
        }

        /// <summary>
        /// Converts a hexagon dimension to a node size.
        ///
        /// A hexagon can be defined using either its diameter, or width, none of which are the same as the <see cref="nodeSize"/> used internally to define the size of a single node.
        ///
        /// See: <see cref="ConvertNodeSizeToHexagonSize"/>
        /// </summary>
        public static float ConvertHexagonSizeToNodeSize(InspectorGridHexagonNodeSize mode, float value)
        {
            return default;
        }

        /// <summary>
        /// Converts an internal node size to a hexagon dimension.
        ///
        /// A hexagon can be defined using either its diameter, or width, none of which are the same as the <see cref="nodeSize"/> used internally to define the size of a single node.
        ///
        /// See: ConvertHexagonSizeToNodeSize
        /// </summary>
        public static float ConvertNodeSizeToHexagonSize(InspectorGridHexagonNodeSize mode, float value)
        {
            return default;
        }

        public int Width {
			get {
				return width;
			}
			set {
				width = value;
			}
		}
		public int Depth {
			get {
				return depth;
			}
			set {
				depth = value;
			}
		}

		/// <summary>
		/// Default cost of moving one node in a particular direction.
		///
		/// Note: You can only call this after the graph has been scanned. Otherwise it will return zero.
		///
		/// <code>
		///         Z
		///         |
		///         |
		///
		///      6  2  5
		///       \ | /
		/// --  3 - X - 1  ----- X
		///       / | \
		///      7  0  4
		///
		///         |
		///         |
		/// </code>
		/// </summary>
		public uint GetConnectionCost (int dir) {
            return default;
        }

        /// <summary>
        /// Changes the grid shape.
        /// This is equivalent to changing the 'shape' dropdown in the grid graph inspector.
        ///
        /// Calling this method will set <see cref="isometricAngle"/>, <see cref="aspectRatio"/>, <see cref="uniformEdgeCosts"/> and <see cref="neighbours"/>
        /// to appropriate values for that shape.
        ///
        /// Note: Setting the shape to <see cref="InspectorGridMode.Advanced"/> does not do anything except set the <see cref="inspectorGridMode"/> field.
        ///
        /// See: <see cref="inspectorHexagonSizeMode"/>
        /// </summary>
        public void SetGridShape (InspectorGridMode shape) {
        }

        /// <summary>
        /// Aligns this grid to a given tilemap or grid layout.
        ///
        /// This is very handy if your game uses a tilemap for rendering and you want to make sure the graph is laid out exactly the same.
        /// Matching grid parameters manually can be quite tricky in some cases.
        ///
        /// The inspector will automatically show a button to align to a tilemap if one is detected in the scene.
        /// If no tilemap is detected, the button be hidden.
        ///
        /// [Open online documentation to see images]
        ///
        /// Note: This will not change the width/height of the graph. It only aligns the graph to the closest orientation so that the grid nodes will be aligned to the cells in the tilemap.
        /// You can adjust the width/height of the graph separately using e.g. <see cref="SetDimensions"/>.
        ///
        /// The following parameters will be updated:
        ///
        /// - <see cref="center"/>
        /// - <see cref="nodeSize"/>
        /// - <see cref="isometricAngle"/>
        /// - <see cref="aspectRatio"/>
        /// - <see cref="rotation"/>
        /// - <see cref="uniformEdgeCosts"/>
        /// - <see cref="neighbours"/>
        /// - <see cref="inspectorGridMode"/>
        /// - <see cref="transform"/>
        ///
        /// See: tilemaps (view in online documentation for working links)
        /// </summary>
        public void AlignToTilemap (UnityEngine.GridLayout grid) {
        }

        /// <summary>
        /// Updates <see cref="unclampedSize"/> from <see cref="width"/>, <see cref="depth"/> and <see cref="nodeSize"/> values.
        /// Also <see cref="UpdateTransform generates a new"/>.
        /// Note: This does not rescan the graph, that must be done with Scan
        ///
        /// You should use this method instead of setting the <see cref="width"/> and <see cref="depth"/> fields
        /// as the grid dimensions are not defined by the <see cref="width"/> and <see cref="depth"/> variables but by
        /// the <see cref="unclampedSize"/> and <see cref="center"/> variables.
        ///
        /// <code>
        /// var gg = AstarPath.active.data.gridGraph;
        /// var width = 80;
        /// var depth = 60;
        /// var nodeSize = 1.0f;
        ///
        /// gg.SetDimensions(width, depth, nodeSize);
        ///
        /// // Recalculate the graph
        /// AstarPath.active.Scan();
        /// </code>
        /// </summary>
        public void SetDimensions(int width, int depth, float nodeSize)
        {
        }

        /// <summary>
        /// Updates the <see cref="transform"/> field which transforms graph space to world space.
        /// In graph space all nodes are laid out in the XZ plane with the first node having a corner in the origin.
        /// One unit in graph space is one node so the first node in the graph is at (0.5,0) the second one at (1.5,0) etc.
        ///
        /// This takes the current values of the parameters such as position and rotation into account.
        /// The transform that was used the last time the graph was scanned is stored in the <see cref="transform"/> field.
        ///
        /// The <see cref="transform"/> field is calculated using this method when the graph is scanned.
        /// The width, depth variables are also updated based on the <see cref="unclampedSize"/> field.
        /// </summary>
        public void UpdateTransform()
        {
        }

        /// <summary>
        /// Returns a new transform which transforms graph space to world space.
        /// Does not update the <see cref="transform"/> field.
        /// See: <see cref="UpdateTransform"/>
        /// </summary>
        public GraphTransform CalculateTransform()
        {
            return default;
        }

        /// <summary>
        /// Calculates the width/depth of the graph from <see cref="unclampedSize"/> and <see cref="nodeSize"/>.
        /// The node size may be changed due to constraints that the width/depth is not
        /// allowed to be larger than 1024 (artificial limit).
        /// </summary>
        void CalculateDimensions(out int width, out int depth, out float nodeSize)
        {
            width = default(int);
            depth = default(int);
            nodeSize = default(float);
        }

        public override float NearestNodeDistanceSqrLowerBound(Vector3 position, NNConstraint constraint)
        {
            return default;
        }

        protected virtual GridNodeBase GetNearestFromGraphSpace(Vector3 positionGraphSpace)
        {
            return default;
        }

        public override NNInfo GetNearest(Vector3 position, NNConstraint constraint, float maxDistanceSqr)
        {
            return default;
        }

        public override NNInfo RandomPointOnSurface(NNConstraint nnConstraint = null, bool highQuality = true)
        {
            return default;
        }

        /// <summary>
        /// Sets up <see cref="neighbourOffsets"/> with the current settings. <see cref="neighbourOffsets"/>, <see cref="neighbourCosts"/>, <see cref="neighbourXOffsets"/> and <see cref="neighbourZOffsets"/> are set up.
        /// The cost for a non-diagonal movement between two adjacent nodes is RoundToInt (<see cref="nodeSize"/> * Int3.Precision)
        /// The cost for a diagonal movement between two adjacent nodes is RoundToInt (<see cref="nodeSize"/> * Sqrt (2) * Int3.Precision)
        /// </summary>
        public virtual void SetUpOffsetsAndCosts()
        {
        }

        public enum RecalculationMode
        {
            /// <summary>Recalculates the nodes from scratch. Used when the graph is first scanned. You should have destroyed all existing nodes before updating the graph with this mode.</summary>
            RecalculateFromScratch,
            /// <summary>Recalculate the minimal number of nodes necessary to guarantee changes inside the graph update's bounding box are taken into account. Some data may be read from the existing nodes</summary>
            RecalculateMinimal,
            /// <summary>Nodes are not recalculated. Used for graph updates which only set node properties</summary>
            NoRecalculation,
        }

        /// <summary>
        /// Moves the grid by a number of nodes.
        ///
        /// This is used by the <see cref="ProceduralGraphMover"/> component to efficiently move the graph.
        ///
        /// All nodes that can stay in the same position will stay. The ones that would have fallen off the edge of the graph will wrap around to the other side
        /// and then be recalculated.
        ///
        /// See: <see cref="ProceduralGraphMover"/>
        ///
        /// Returns: An async graph update promise. See <see cref="IGraphUpdatePromise"/>.
        /// </summary>
        /// <param name="dx">Number of nodes along the graph's X axis to move by.</param>
        /// <param name="dz">Number of nodes along the graph's Z axis to move by.</param>
        public IGraphUpdatePromise TranslateInDirection(int dx, int dz) => new GridGraphMovePromise(this, dx, dz);

        class GridGraphMovePromise : IGraphUpdatePromise
        {
            public GridGraph graph;
            public int dx;
            public int dz;
            IGraphUpdatePromise[] promises;
            IntRect[] rects;
            int3 startingSize;

            static void DecomposeInsetsToRectangles(int width, int height, int insetLeft, int insetRight, int insetBottom, int insetTop, IntRect[] output)
            {
            }

            public GridGraphMovePromise(GridGraph graph, int dx, int dz)
            {
            }

            public IEnumerator<JobHandle> Prepare()
            {
                return default;
            }

            public void Apply(IGraphUpdateContext ctx)
            {
            }
        }

        class GridGraphUpdatePromise : IGraphUpdatePromise
        {
            /// <summary>Reference to a nodes array to allow multiple serial updates to have a common reference to the nodes</summary>
            public class NodesHolder
            {
                public GridNodeBase[] nodes;
            }
            public GridGraph graph;
            public NodesHolder nodes;
            public JobDependencyTracker dependencyTracker;
            public int3 nodeArrayBounds;
            public IntRect rect;
            public JobHandle nodesDependsOn;
            public Allocator allocationMethod;
            public RecalculationMode recalculationMode;
            public GraphUpdateObject graphUpdateObject;
            IntBounds writeMaskBounds;
            internal GridGraphRules.Context context;
            bool emptyUpdate;
            IntBounds readBounds;
            IntBounds fullRecalculationBounds;
            public bool ownsJobDependencyTracker = false;
            bool isFinalUpdate;
            GraphTransform transform;

            public int CostEstimate => fullRecalculationBounds.volume;

            public GridGraphUpdatePromise(GridGraph graph, GraphTransform transform, NodesHolder nodes, int3 nodeArrayBounds, IntRect rect, JobDependencyTracker dependencyTracker, JobHandle nodesDependsOn, Allocator allocationMethod, RecalculationMode recalculationMode, GraphUpdateObject graphUpdateObject, bool ownsJobDependencyTracker, bool isFinalUpdate)
            {
            }

            /// <summary>Calculates the rectangles used for different purposes during a graph update.</summary>
            /// <param name="graph">The graph</param>
            /// <param name="rect">The rectangle to update. Anything inside this rectangle may have changed (which may affect nodes outside this rectangle as well).</param>
            /// <param name="originalRect">The original rectangle passed to the update method, clamped to the grid.</param>
            /// <param name="fullRecalculationRect">The rectangle of nodes which will be recalculated from scratch.</param>
            /// <param name="writeMaskRect">The rectangle of nodes which will have their results written back to the graph.</param>
            /// <param name="readRect">The rectangle of nodes which we need to read from in order to recalculate all nodes in writeMaskRect correctly.</param>
            public static void CalculateRectangles(GridGraph graph, IntRect rect, out IntRect originalRect, out IntRect fullRecalculationRect, out IntRect writeMaskRect, out IntRect readRect)
            {
                originalRect = default(IntRect);
                fullRecalculationRect = default(IntRect);
                writeMaskRect = default(IntRect);
                readRect = default(IntRect);
            }

            public IEnumerator<JobHandle> Prepare()
            {
                return default;
            }

            public void Apply(IGraphUpdateContext ctx)
            {
            }

            public void Dispose()
            {
            }
        }

        protected override IGraphUpdatePromise ScanInternal(bool async)
        {
            return default;
        }

        /// <summary>
        /// Set walkability for multiple nodes at once.
        ///
        /// If you are calculating your graph's walkability in some custom way, you can use this method to copy that data to the graph.
        /// In most cases you'll not use this method, but instead build your world with colliders and such, and then scan the graph.
        ///
        /// Note: Any other graph updates may overwrite this data.
        ///
        /// <code>
        /// // Perform the update when it is safe to do so
        /// AstarPath.active.AddWorkItem(() => {
        ///     var grid = AstarPath.active.data.gridGraph;
        ///     // Mark all nodes in a 10x10 square, in the top-left corner of the graph, as unwalkable.
        ///     grid.SetWalkability(new bool[10*10], new IntRect(0, 0, 9, 9));
        /// });
        /// </code>
        ///
        /// See: grid-rules (view in online documentation for working links) for an alternative way of modifying the graph's walkability. It is more flexible and robust, but requires a bit more code.
        /// </summary>
        public void SetWalkability(bool[] walkability, IntRect rect)
        {
        }

        /// <summary>
        /// Recalculates node connections for all nodes in grid graph.
        ///
        /// This is used if you have manually changed the walkability, or other parameters, of some grid nodes, and you need their connections to be recalculated.
        /// If you are changing the connections themselves, you should use the <see cref="GraphNode.Connect"/> and <see cref="GraphNode.Disconnect"/> functions instead.
        ///
        /// Typically you do not change walkability manually. Instead you can use for example a <see cref="GraphUpdateObject"/>.
        ///
        /// Note: This will not take into account any grid graph rules that modify connections. So if you have any of those added to the grid graph, you probably want to do a regular graph update instead.
        ///
        /// See: graph-updates (view in online documentation for working links)
        /// See: <see cref="CalculateConnectionsForCellAndNeighbours"/>
        /// See: <see cref="RecalculateConnectionsInRegion"/>
        /// </summary>
        public void RecalculateAllConnections () {
        }

        /// <summary>
        /// Recalculates node connections for all nodes in a given region of the grid.
        ///
        /// This is used if you have manually changed the walkability, or other parameters, of some grid nodes, and you need their connections to be recalculated.
        /// If you are changing the connections themselves, you should use the <see cref="GraphNode.AddConnection"/> and <see cref="GraphNode.RemoveConnection"/> functions instead.
        ///
        /// Typically you do not change walkability manually. Instead you can use for example a <see cref="GraphUpdateObject"/>.
        ///
        /// Warning: This method has some constant overhead, so if you are making several changes to the graph, it is best to batch these updates and only make a single call to this method.
        ///
        /// Note: This will not take into account any grid graph rules that modify connections. So if you have any of those added to the grid graph, you probably want to do a regular graph update instead.
        ///
        /// See: graph-updates (view in online documentation for working links)
        /// See: <see cref="RecalculateAllConnections"/>
        /// See: <see cref="CalculateConnectionsForCellAndNeighbours"/>
        /// </summary>
        public void RecalculateConnectionsInRegion (IntRect recalculateRect) {
        }

        /// <summary>
        /// Calculates the grid connections for a cell as well as its neighbours.
        /// This is a useful utility function if you want to modify the walkability of a single node in the graph.
        ///
        /// <code>
        /// AstarPath.active.AddWorkItem(ctx => {
        ///     var grid = AstarPath.active.data.gridGraph;
        ///     int x = 5;
        ///     int z = 7;
        ///
        ///     // Mark a single node as unwalkable
        ///     grid.GetNode(x, z).Walkable = false;
        ///
        ///     // Recalculate the connections for that node as well as its neighbours
        ///     grid.CalculateConnectionsForCellAndNeighbours(x, z);
        /// });
        /// </code>
        ///
        /// Warning: If you are recalculating connections for a lot of nodes at the same time, use <see cref="RecalculateConnectionsInRegion"/> instead, since that will be much faster.
        /// </summary>
        public void CalculateConnectionsForCellAndNeighbours (int x, int z) {
        }

        /// <summary>
        /// Calculates the grid connections for a single node.
        /// Convenience function, it's slightly faster to use CalculateConnections(int,int)
        /// but that will only show when calculating for a large number of nodes.
        /// This function will also work for both grid graphs and layered grid graphs.
        ///
        /// Deprecated: This method is very slow since 4.3.80. Use <see cref="RecalculateConnectionsInRegion"/> or <see cref="RecalculateAllConnections"/> instead to batch connection recalculations.
        /// </summary>
        [System.Obsolete("This method is very slow since 4.3.80. Use RecalculateConnectionsInRegion or RecalculateAllConnections instead to batch connection recalculations.")]
        public virtual void CalculateConnections(GridNodeBase node)
        {
        }

        /// <summary>
        /// Calculates the grid connections for a single node.
        /// Note that to ensure that connections are completely up to date after updating a node you
        /// have to calculate the connections for both the changed node and its neighbours.
        ///
        /// In a layered grid graph, this will recalculate the connections for all nodes
        /// in the (x,z) cell (it may have multiple layers of nodes).
        ///
        /// See: CalculateConnections(GridNodeBase)
        ///
        /// Deprecated: This method is very slow since 4.3.80. Use <see cref="RecalculateConnectionsInRegion"/> instead to batch connection recalculations.
        /// </summary>
        [System.Obsolete("This method is very slow since 4.3.80. Use RecalculateConnectionsInRegion instead to batch connection recalculations.")]
        public virtual void CalculateConnections(int x, int z)
        {
        }

        public override void OnDrawGizmos(DrawingData gizmos, bool drawNodes, RedrawScope redrawScope)
        {
        }

        /// <summary>
        /// Draw the surface as well as an outline of the grid graph.
        /// The nodes will be drawn as squares (or hexagons when using <see cref="neighbours"/> = Six).
        /// </summary>
        void CreateNavmeshSurfaceVisualization(GridNodeBase[] nodes, int nodeCount, GraphGizmoHelper helper)
        {
        }

        /// <summary>
        /// Bounding box in world space which encapsulates all nodes in the given rectangle.
        ///
        /// The bounding box will cover all nodes' surfaces completely. Not just their centers.
        ///
        /// Note: The bounding box may not be particularly tight if the graph is not axis-aligned.
        ///
        /// See: <see cref="GetRectFromBounds"/>
        /// </summary>
        /// <param name="rect">Which nodes to consider. Will be clamped to the grid's bounds. If the rectangle is outside the graph, an empty bounds will be returned.</param>
        public Bounds GetBoundsFromRect(IntRect rect)
        {
            return default;
        }

        /// <summary>
        /// A rect that contains all nodes that the bounds could touch.
        /// This correctly handles rotated graphs and other transformations.
        /// The returned rect is guaranteed to not extend outside the graph bounds.
        ///
        /// Note: The rect may contain nodes that are not contained in the bounding box since the bounding box is aligned to the world, and the rect is aligned to the grid (which may be rotated).
        ///
        /// See: <see cref="GetNodesInRegion(Bounds)"/>
        /// See: <see cref="GetNodesInRegion(IntRect)"/>
        /// </summary>
        public IntRect GetRectFromBounds(Bounds bounds)
        {
            return default;
        }

        /// <summary>
        /// All nodes inside the bounding box.
        /// Note: Be nice to the garbage collector and pool the list when you are done with it (optional)
        /// See: Pathfinding.Pooling.ListPool
        ///
        /// See: GetNodesInRegion(GraphUpdateShape)
        /// </summary>
        public List<GraphNode> GetNodesInRegion(Bounds bounds)
        {
            return default;
        }

        /// <summary>
        /// All nodes inside the shape.
        /// Note: Be nice to the garbage collector and pool the list when you are done with it (optional)
        /// See: Pathfinding.Pooling.ListPool
        ///
        /// See: GetNodesInRegion(Bounds)
        /// </summary>
        public List<GraphNode> GetNodesInRegion(GraphUpdateShape shape)
        {
            return default;
        }

        /// <summary>
        /// All nodes inside the shape or if null, the bounding box.
        /// If a shape is supplied, it is assumed to be contained inside the bounding box.
        /// See: GraphUpdateShape.GetBounds
        /// </summary>
        protected virtual List<GraphNode> GetNodesInRegion(Bounds bounds, GraphUpdateShape shape)
        {
            return default;
        }

        /// <summary>
        /// Get all nodes in a rectangle.
        ///
        /// See: <see cref="GetRectFromBounds"/>
        /// </summary>
        /// <param name="rect">Region in which to return nodes. It will be clamped to the grid.</param>
        public List<GraphNode> GetNodesInRegion(IntRect rect)
        {
            return default;
        }

        /// <summary>
        /// Get all nodes in a rectangle.
        /// Returns: The number of nodes written to the buffer.
        ///
        /// Note: This method is much faster than GetNodesInRegion(IntRect) which returns a list because this method can make use of the highly optimized
        ///  System.Array.Copy method.
        ///
        /// See: <see cref="GetRectFromBounds"/>
        /// </summary>
        /// <param name="rect">Region in which to return nodes. It will be clamped to the grid.</param>
        /// <param name="buffer">Buffer in which the nodes will be stored. Should be at least as large as the number of nodes that can exist in that region.</param>
        public virtual int GetNodesInRegion(IntRect rect, GridNodeBase[] buffer)
        {
            return default;
        }

        /// <summary>
        /// Node in the specified cell.
        /// Returns null if the coordinate is outside the grid.
        ///
        /// <code>
        /// var gg = AstarPath.active.data.gridGraph;
        /// int x = 5;
        /// int z = 8;
        /// GridNodeBase node = gg.GetNode(x, z);
        /// </code>
        ///
        /// If you know the coordinate is inside the grid and you are looking to maximize performance then you
        /// can look up the node in the internal array directly which is slightly faster.
        /// See: <see cref="nodes"/>
        /// </summary>
        public virtual GridNodeBase GetNode(int x, int z)
        {
            return default;
        }

        class CombinedGridGraphUpdatePromise : IGraphUpdatePromise
        {
            List<IGraphUpdatePromise> promises;

            public CombinedGridGraphUpdatePromise(GridGraph graph, List<GraphUpdateObject> graphUpdates)
            {
            }

            public IEnumerator<JobHandle> Prepare()
            {
                return default;
            }

            public void Apply(IGraphUpdateContext ctx)
            {
            }
        }

        /// <summary>Internal function to update the graph</summary>
        IGraphUpdatePromise IUpdatableGraph.ScheduleGraphUpdates(List<GraphUpdateObject> graphUpdates)
        {
            return default;
        }

        class GridGraphSnapshot : IGraphSnapshot
        {
            internal GridGraphNodeData nodes;
            internal GridGraph graph;

            public void Dispose()
            {
            }

            public void Restore(IGraphUpdateContext ctx)
            {
            }
        }

		public override IGraphSnapshot Snapshot(Bounds bounds)
        {
            return default;
        }

        /// <summary>
        /// Returns if there is an obstacle between from and to on the graph.
        /// This is not the same as Physics.Linecast, this function traverses the graph and looks for collisions.
        ///
        /// <code>
        /// var gg = AstarPath.active.data.gridGraph;
        /// bool anyObstaclesInTheWay = gg.Linecast(transform.position, enemy.position);
        /// </code>
        ///
        /// [Open online documentation to see images]
        ///
        /// Edge cases are handled as follows:
        /// - Shared edges and corners between walkable and unwalkable nodes are treated as walkable (so for example if the linecast just touches a corner of an unwalkable node, this is allowed).
        /// - If the linecast starts outside the graph, a hit is returned at from.
        /// - If the linecast starts inside the graph, but the end is outside of it, a hit is returned at the point where it exits the graph (unless there are any other hits before that).
        /// </summary>
        public bool Linecast(Vector3 from, Vector3 to)
        {
            return default;
        }

        /// <summary>
        /// Returns if there is an obstacle between from and to on the graph.
        ///
        /// This is not the same as Physics.Linecast, this function traverses the graph and looks for collisions.
        ///
        /// <code>
        /// var gg = AstarPath.active.data.gridGraph;
        /// bool anyObstaclesInTheWay = gg.Linecast(transform.position, enemy.position);
        /// </code>
        ///
        /// [Open online documentation to see images]
        ///
        /// Deprecated: The hint parameter is deprecated
        /// </summary>
        /// <param name="from">Point to linecast from</param>
        /// <param name="to">Point to linecast to</param>
        /// <param name="hint">This parameter is deprecated. It will be ignored.</param>
        [System.Obsolete("The hint parameter is deprecated")]
        public bool Linecast(Vector3 from, Vector3 to, GraphNode hint)
        {
            return default;
        }

        /// <summary>
        /// Returns if there is an obstacle between from and to on the graph.
        ///
        /// This is not the same as Physics.Linecast, this function traverses the graph and looks for collisions.
        ///
        /// <code>
        /// var gg = AstarPath.active.data.gridGraph;
        /// bool anyObstaclesInTheWay = gg.Linecast(transform.position, enemy.position);
        /// </code>
        ///
        /// [Open online documentation to see images]
        ///
        /// Deprecated: The hint parameter is deprecated
        /// </summary>
        /// <param name="from">Point to linecast from</param>
        /// <param name="to">Point to linecast to</param>
        /// <param name="hit">Contains info on what was hit, see GraphHitInfo</param>
        /// <param name="hint">This parameter is deprecated. It will be ignored.</param>
        [System.Obsolete("The hint parameter is deprecated")]
        public bool Linecast(Vector3 from, Vector3 to, GraphNode hint, out GraphHitInfo hit)
        {
            hit = default(GraphHitInfo);
            return default;
        }

        /// <summary>Magnitude of the cross product a x b</summary>
        protected static long CrossMagnitude(int2 a, int2 b)
        {
            return default;
        }

        /// <summary>
        /// Clips a line segment in graph space to the graph bounds.
        /// That is (0,0,0) is the bottom left corner of the graph and (width,0,depth) is the top right corner.
        /// The first node is placed at (0.5,y,0.5). One unit distance is the same as nodeSize.
        ///
        /// Returns false if the line segment does not intersect the graph at all.
        /// </summary>
        protected bool ClipLineSegmentToBounds (Vector3 a, Vector3 b, out Vector3 outA, out Vector3 outB) {
            outA = default(Vector3);
            outB = default(Vector3);
            return default;
        }

        /// <summary>
        /// Returns if there is an obstacle between from and to on the graph.
        ///
        /// This is not the same as Physics.Linecast, this function traverses the graph and looks for collisions.
        ///
        /// <code>
        /// var gg = AstarPath.active.data.gridGraph;
        /// bool anyObstaclesInTheWay = gg.Linecast(transform.position, enemy.position);
        /// </code>
        ///
        /// Deprecated: The hint parameter is deprecated
        ///
        /// [Open online documentation to see images]
        /// </summary>
        /// <param name="from">Point to linecast from</param>
        /// <param name="to">Point to linecast to</param>
        /// <param name="hit">Contains info on what was hit, see GraphHitInfo</param>
        /// <param name="hint">This parameter is deprecated. It will be ignored.</param>
        /// <param name="trace">If a list is passed, then it will be filled with all nodes the linecast traverses</param>
        /// <param name="filter">If not null then the delegate will be called for each node and if it returns false the node will be treated as unwalkable and a hit will be returned.
        ///               Note that unwalkable nodes are always treated as unwalkable regardless of what this filter returns.</param>
        [System.Obsolete("The hint parameter is deprecated")]
        public bool Linecast(Vector3 from, Vector3 to, GraphNode hint, out GraphHitInfo hit, List<GraphNode> trace, System.Func<GraphNode, bool> filter = null)
        {
            hit = default(GraphHitInfo);
            return default;
        }

        /// <summary>
        /// Returns if there is an obstacle between from and to on the graph.
        ///
        /// This is not the same as Physics.Linecast, this function traverses the graph and looks for collisions.
        ///
        /// Edge cases are handled as follows:
        /// - Shared edges and corners between walkable and unwalkable nodes are treated as walkable (so for example if the linecast just touches a corner of an unwalkable node, this is allowed).
        /// - If the linecast starts outside the graph, a hit is returned at from.
        /// - If the linecast starts inside the graph, but the end is outside of it, a hit is returned at the point where it exits the graph (unless there are any other hits before that).
        ///
        /// <code>
        /// var gg = AstarPath.active.data.gridGraph;
        /// bool anyObstaclesInTheWay = gg.Linecast(transform.position, enemy.position);
        /// </code>
        ///
        /// [Open online documentation to see images]
        /// </summary>
        /// <param name="from">Point to linecast from</param>
        /// <param name="to">Point to linecast to</param>
        /// <param name="hit">Contains info on what was hit, see \reflink{GraphHitInfo}.</param>
        /// <param name="trace">If a list is passed, then it will be filled with all nodes the linecast traverses</param>
        /// <param name="filter">If not null then the delegate will be called for each node and if it returns false the node will be treated as unwalkable and a hit will be returned.
        ///               Note that unwalkable nodes are always treated as unwalkable regardless of what this filter returns.</param>
        public bool Linecast(Vector3 from, Vector3 to, out GraphHitInfo hit, List<GraphNode> trace = null, System.Func<GraphNode, bool> filter = null)
        {
            hit = default(GraphHitInfo);
            return default;
        }

        /// <summary>
        /// Returns if there is an obstacle between from and to on the graph.
        ///
        /// This function is different from the other Linecast functions since it snaps the start and end positions to the centers of the closest nodes on the graph.
        /// This is not the same as Physics.Linecast, this function traverses the graph and looks for collisions.
        ///
        /// Version: Since 3.6.8 this method uses the same implementation as the other linecast methods so there is no performance boost to using it.
        /// Version: In 3.6.8 this method was rewritten and that fixed a large number of bugs.
        /// Previously it had not always followed the line exactly as it should have
        /// and the hit output was not very accurate
        /// (for example the hit point was just the node position instead of a point on the edge which was hit).
        ///
        /// Deprecated: Use <see cref="Linecast"/> instead.
        /// </summary>
        /// <param name="from">Point to linecast from.</param>
        /// <param name="to">Point to linecast to.</param>
        /// <param name="hit">Contains info on what was hit, see GraphHitInfo.</param>
        /// <param name="hint">This parameter is deprecated. It will be ignored.</param>
        [System.Obsolete("Use Linecast instead")]
        public bool SnappedLinecast(Vector3 from, Vector3 to, GraphNode hint, out GraphHitInfo hit)
        {
            hit = default(GraphHitInfo);
            return default;
        }

        /// <summary>
        /// Returns if there is an obstacle between the two nodes on the graph.
        ///
        /// This method is very similar to the other Linecast methods however it is a bit faster
        /// due to not having to look up which node is closest to a particular input point.
        ///
        /// <code>
        /// var gg = AstarPath.active.data.gridGraph;
        /// var node1 = gg.GetNode(2, 3);
        /// var node2 = gg.GetNode(5, 7);
        /// bool anyObstaclesInTheWay = gg.Linecast(node1, node2);
        /// </code>
        /// </summary>
        /// <param name="fromNode">Node to start from.</param>
        /// <param name="toNode">Node to try to reach using a straight line.</param>
        /// <param name="filter">If not null then the delegate will be called for each node and if it returns false the node will be treated as unwalkable and a hit will be returned.
        ///               Note that unwalkable nodes are always treated as unwalkable regardless of what this filter returns.</param>
        public bool Linecast(GridNodeBase fromNode, GridNodeBase toNode, System.Func<GraphNode, bool> filter = null)
        {
            return default;
        }

        /// <summary>
        /// Returns if there is an obstacle between from and to on the graph.
        ///
        /// This is not the same as Physics.Linecast, this function traverses the graph and looks for collisions.
        ///
        /// Note: This overload outputs a hit of type <see cref="GridHitInfo"/> instead of <see cref="GraphHitInfo"/>. It's a bit faster to calculate this output
        /// and it can be useful for some grid-specific algorithms.
        ///
        /// Edge cases are handled as follows:
        /// - Shared edges and corners between walkable and unwalkable nodes are treated as walkable (so for example if the linecast just touches a corner of an unwalkable node, this is allowed).
        /// - If the linecast starts outside the graph, a hit is returned at from.
        /// - If the linecast starts inside the graph, but the end is outside of it, a hit is returned at the point where it exits the graph (unless there are any other hits before that).
        ///
        /// <code>
        /// var gg = AstarPath.active.data.gridGraph;
        /// bool anyObstaclesInTheWay = gg.Linecast(transform.position, enemy.position);
        /// </code>
        ///
        /// [Open online documentation to see images]
        /// </summary>
        /// <param name="from">Point to linecast from</param>
        /// <param name="to">Point to linecast to</param>
        /// <param name="hit">Contains info on what was hit, see \reflink{GridHitInfo}</param>
        /// <param name="trace">If a list is passed, then it will be filled with all nodes the linecast traverses</param>
        /// <param name="filter">If not null then the delegate will be called for each node and if it returns false the node will be treated as unwalkable and a hit will be returned.
        ///               Note that unwalkable nodes are always treated as unwalkable regardless of what this filter returns.</param>
        public bool Linecast(Vector3 from, Vector3 to, out GridHitInfo hit, List<GraphNode> trace = null, System.Func<GraphNode, bool> filter = null)
        {
            hit = default(GridHitInfo);
            return default;
        }

        /// <summary>
        /// Scaling used for the coordinates in the Linecast methods that take normalized points using integer coordinates.
        ///
        /// To convert from world space, each coordinate is multiplied by this factor and then rounded to the nearest integer.
        ///
        /// Typically you do not need to use this constant yourself, instead use the Linecast overloads that do not take integer coordinates.
        /// </summary>
        public const int FixedPrecisionScale = 1024;

        /// <summary>
        /// Returns if there is an obstacle between the two nodes on the graph.
        ///
        /// This method is very similar to the other Linecast methods but it gives some extra control, in particular when the start/end points are at node corners instead of inside nodes.
        ///
        /// Shared edges and corners between walkable and unwalkable nodes are treated as walkable.
        /// So for example if the linecast just touches a corner of an unwalkable node, this is allowed.
        /// </summary>
        /// <param name="fromNode">Node to start from.</param>
        /// <param name="normalizedFromPoint">Where in the start node to start. This is a normalized value so each component must be in the range 0 to 1 (inclusive).</param>
        /// <param name="toNode">Node to try to reach using a straight line.</param>
        /// <param name="normalizedToPoint">Where in the end node to end. This is a normalized value so each component must be in the range 0 to 1 (inclusive).</param>
        /// <param name="hit">Contains info on what was hit, see \reflink{GridHitInfo}</param>
        /// <param name="trace">If a list is passed, then it will be filled with all nodes the linecast traverses</param>
        /// <param name="filter">If not null then the delegate will be called for each node and if it returns false the node will be treated as unwalkable and a hit will be returned.
        ///               Note that unwalkable nodes are always treated as unwalkable regardless of what this filter returns.</param>
        /// <param name="continuePastEnd">If true, the linecast will continue past the end point in the same direction until it hits something.</param>
        public bool Linecast(GridNodeBase fromNode, Vector2 normalizedFromPoint, GridNodeBase toNode, Vector2 normalizedToPoint, out GridHitInfo hit, List<GraphNode> trace = null, System.Func<GraphNode, bool> filter = null, bool continuePastEnd = false)
        {
            hit = default(GridHitInfo);
            return default;
        }

        /// <summary>
        /// Returns if there is an obstacle between the two nodes on the graph.
        /// Like <see cref="Linecast(GridNodeBase,Vector2,GridNodeBase,Vector2,GridHitInfo,List<GraphNode>,System.Func<GraphNode,bool>,bool)"/> but takes normalized points as fixed precision points normalized between 0 and FixedPrecisionScale instead of between 0 and 1.
        /// </summary>
        public bool Linecast(GridNodeBase fromNode, int2 fixedNormalizedFromPoint, GridNodeBase toNode, int2 fixedNormalizedToPoint, out GridHitInfo hit, List<GraphNode> trace = null, System.Func<GraphNode, bool> filter = null, bool continuePastEnd = false)
        {
            hit = default(GridHitInfo);
            return default;
        }

        protected override void SerializeExtraInfo(GraphSerializationContext ctx)
        {
        }

        protected override void DeserializeExtraInfo(GraphSerializationContext ctx)
        {
        }

        protected void DeserializeNativeData(GraphSerializationContext ctx, bool normalsSerialized)
        {
        }

        protected void SerializeNodeSurfaceNormals(GraphSerializationContext ctx)
        {
        }

        protected void DeserializeNodeSurfaceNormals(GraphSerializationContext ctx, GridNodeBase[] nodes, bool ignoreForCompatibility)
        {
        }

        void HandleBackwardsCompatibility(GraphSerializationContext ctx)
        {
        }

        protected override void PostDeserialization(GraphSerializationContext ctx)
        {
        }
    }

	/// <summary>
	/// Number of neighbours for a single grid node.
	/// Since: The 'Six' item was added in 3.6.1
	/// </summary>
	public enum NumNeighbours {
		Four,
		Eight,
		Six
	}

	/// <summary>Information about a linecast hit on a grid graph</summary>
	public struct GridHitInfo {
		/// <summary>
		/// The node which contained the edge that was hit.
		/// This may be null in case no particular edge was hit.
		/// </summary>
		public GridNodeBase node;
		/// <summary>
		/// Direction from the node to the edge that was hit.
		/// This will be in the range of 0 to 4 (exclusive) or -1 if no particular edge was hit.
		///
		/// See: <see cref="GridNodeBase.GetNeighbourAlongDirection"/>
		/// </summary>
		public int direction;
	}
}
