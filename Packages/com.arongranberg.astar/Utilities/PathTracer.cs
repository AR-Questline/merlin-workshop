using System.Collections.Generic;
using UnityEngine;
using Pathfinding.Util;
using UnityEngine.Assertions;
using Unity.Mathematics;
using Pathfinding.Drawing;
using Unity.Collections;
using Unity.Burst;
using UnityEngine.Profiling;
using Unity.Profiling;
using System.Runtime.CompilerServices;
using Unity.Jobs.LowLevel.Unsafe;
using Pathfinding.Collections;
using Pathfinding.Pooling;

namespace Pathfinding {
	/// <summary>
	/// Helper for following a path.
	///
	/// This struct keeps track of the path from the agent's current position to the end of the path.
	/// Whenever the agent moves you should call <see cref="UpdateStart"/> to update the path. This will automatically
	/// update the path if the agent has moved to the next node, or repair the path if the agent has been pushed
	/// away into a node which wasn't even on the original path.
	/// If the destination changes you should call <see cref="UpdateEnd"/> to update the path. This also repairs the path
	/// and it allows you to instantly get a valid path to the new destination, unless the destination has
	/// changed so much that the repair is insufficient. In that case you will have to wait for the next
	/// path recalculation. Check <see cref="isStale"/> to see if the PathTracer recommends that the path be recalculated.
	///
	/// After repairing the path, it will be valid, but it will not necessarily be the shortest possible path.
	/// Therefore it is still recommended that you recalculate the path every now and then.
	///
	/// The PathTracer stores the current path as a series of nodes. When the direction to move in is requested (by calling <see cref="GetNextCorners)"/>,
	/// a path will be found which passes through all those nodes, using the funnel algorithm to simplify the path.
	/// In some cases the path will contain inner vertices which make the path longer than it needs to be. Those will be
	/// iteratively removed until the path is as short as possible. For performance only a limited number of iterations are performed per frame,
	/// but this is usually fast enough that the simplification appears to be instant.
	///
	/// Warning: This struct allocates unmanaged memory. You must call <see cref="Dispose"/> when you are done with it, to avoid memory leaks.
	///
	/// Note: This is a struct, not a class. This means that if you need to pass it around, or return it from a property, you must use the ref keyword, as otherwise C# will just make a copy.
	///
	/// <code>
	/// using Pathfinding;
	/// using Pathfinding.Drawing;
	/// using Pathfinding.Util;
	/// using Unity.Collections;
	/// using Unity.Mathematics;
	/// using UnityEngine;
	///
	/// /** Demonstrates how to use a PathTracer.
	///  *
	///  * The script will calculate a path to a point a few meters ahead of it, and then use the PathTracer to show the next 10 corners of the path in the scene view.
	///  * If you move the object around in the scene view, you'll see the path update in real time.
	///  */
	/// public class PathTracerTest : MonoBehaviour {
	///     PathTracer tracer;
	///
	///     /** Create a new movement plane that indicates that the agent moves in the XZ plane.
	///      * This is the default for 3D games.
	///      */
	///     NativeMovementPlane movementPlane => new NativeMovementPlane(Quaternion.identity);
	///     ABPath lastCalculatedPath;
	///     public PathRequestSettings pathRequestSettings = PathRequestSettings.Default;
	///
	///     void OnEnable () {
	///         tracer = new PathTracer(Allocator.Persistent);
	///     }
	///
	///     void OnDisable () {
	///         // Release all unmanaged memory from the path tracer, to avoid memory leaks
	///         tracer.Dispose();
	///     }
	///
	///     void Start () {
	///         // Schedule a path calculation to a point ahead of this object
	///         var path = ABPath.Construct(transform.position, transform.position + transform.forward*10, (p) => {
	///             // This callback will be called when the path has been calculated
	///             var path = p as ABPath;
	///
	///             if (path.error) {
	///                 // The path could not be calculated
	///                 Debug.LogError("Could not calculate path");
	///                 return;
	///             }
	///
	///             // Split the path into normal sequences of nodes, and off-mesh links
	///             var parts = Funnel.SplitIntoParts(path);
	///
	///             // Assign the path to the PathTracer
	///             tracer.SetPath(parts, path.path, path.originalStartPoint, path.originalEndPoint, movementPlane, pathRequestSettings, path);
	///             lastCalculatedPath = path;
	///         });
	///         path.UseSettings(pathRequestSettings);
	///         AstarPath.StartPath(path);
	///     }
	///
	///     void Update () {
	///         if (lastCalculatedPath == null || !tracer.isCreated) return;
	///
	///         // Repair the path to start from the transform's position
	///         // If you move the transform around in the scene view, you'll see the path update in real time
	///         tracer.UpdateStart(transform.position, PathTracer.RepairQuality.High, movementPlane, lastCalculatedPath.traversalProvider, lastCalculatedPath);
	///
	///         // Get up to the next 10 corners of the path
	///         var buffer = new NativeList<float3>(Allocator.Temp);
	///         NativeArray<int> scratchArray = default;
	///         tracer.GetNextCorners(buffer, 10, ref scratchArray, Allocator.Temp, lastCalculatedPath.traversalProvider, lastCalculatedPath);
	///
	///         // Draw the next 10 corners of the path in the scene view
	///         using (Draw.WithLineWidth(2)) {
	///             Draw.Polyline(buffer.AsArray(), Color.red);
	///         }
	///
	///         // Release all temporary unmanaged memory
	///         buffer.Dispose();
	///         scratchArray.Dispose();
	///     }
	/// }
	/// </code>
	/// </summary>
	[BurstCompile]
	public struct PathTracer {
		Funnel.PathPart[] parts;

		/// <summary>All nodes in the path</summary>
		CircularBuffer<GraphNode> nodes;

		/// <summary>
		/// Hashes of some important data for each node, to determine if the node has been invalidated in some way.
		///
		/// For e.g. the grid graph, this is done using the node's index in the grid. This ensures that the node is counted as invalid
		/// if the node is for example moved to the other side of the graph using the <see cref="ProceduralGraphMover"/>.
		///
		/// For all nodes, this includes if info about if the node has been destroyed, and if it is walkable.
		///
		/// This will always have the same length as the <see cref="nodes"/> array, and the absolute indices in this array will correspond to the absolute indices in the <see cref="nodes"/> array.
		/// </summary>
		CircularBuffer<int> nodeHashes;

		/// <summary>
		/// Indicates if portals are definitely not inner corners, or if they may be.
		/// For each portal, if bit 0 is set then the left side of the portal is definitely not an inner corner.
		/// If bit 1 is set that means the same thing but for the right side of the portal.
		///
		/// Should always have the same length as the portals in <see cref="funnelState"/>.
		/// </summary>
		CircularBuffer<byte> portalIsNotInnerCorner;

		Funnel.FunnelState funnelState;
		Vector3 unclampedEndPoint;
		Vector3 unclampedStartPoint;
		GraphNode startNodeInternal;

		NNConstraint nnConstraint;

		int firstPartIndex;
		bool startIsUpToDate;
		bool endIsUpToDate;

		/// <summary>
		/// If true, the first part contains destroyed nodes.
		/// This can happen if the graph is updated and some nodes are destroyed.
		///
		/// If this is true, the path is considered stale and should be recalculated.
		///
		/// The opposite is not necessarily true. If this is false, the path may still be stale.
		///
		/// See: <see cref="isStale"/>
		/// </summary>
		bool firstPartContainsDestroyedNodes;

		/// <summary>
		/// The type of graph that the current path part is on.
		///
		/// This is either a grid-like graph, or a navmesh-like graph.
		/// </summary>
		public PartGraphType partGraphType;

		/// <summary>Type of graph that the current path part is on</summary>
		public enum PartGraphType : byte {
			/// <summary>
			/// A navmesh-like graph.
			///
			/// Typically either a <see cref="NavMeshGraph"/> or a <see cref="RecastGraph"/>
			/// </summary>
			Navmesh,
			/// <summary>
			/// A grid-like graph.
			///
			/// Typically either a <see cref="GridGraph"/> or a <see cref="LayerGridGraph"/>
			/// </summary>
			Grid,
			OffMeshLink,
		}

		/// <summary>Incremented whenever the path is changed</summary>
		public ushort version { [IgnoredByDeepProfiler] get; [IgnoredByDeepProfiler] private set; }

		/// <summary>True until <see cref="Dispose"/> is called</summary>
		public readonly bool isCreated => funnelState.unwrappedPortals.IsCreated;

		/// <summary>
		/// Current start node of the path.
		/// Since the path is updated every time the agent moves, this will be the node which the agent is inside.
		///
		/// In case the path has become invalid, this will be set to the closest node to the agent, or if no such node could be found, it will be set to null.
		///
		/// Note: Not necessarily up to date unless <see cref="UpdateStart"/> has been called first.
		/// </summary>
		public GraphNode startNode {
			[IgnoredByDeepProfiler]
			readonly get => startNodeInternal != null && !startNodeInternal.Destroyed ? startNodeInternal : null;
			[IgnoredByDeepProfiler]
			private set => startNodeInternal = value;
		}

		/// <summary>
		/// True if the path is stale and should be recalculated as quickly as possible.
		/// This is true if the path has become invalid (e.g. due to a graph update), or if the destination has changed so much that we don't have a path to the destination at all.
		///
		/// For performance reasons, the agent tries to avoid checking if nodes have been destroyed unless it needs to access them to calculate its movement.
		/// Therefore, if a path is invalidated further ahead, the agent may not realize this until it has moved close enough.
		/// </summary>
		public readonly bool isStale {
			[IgnoredByDeepProfiler]
			get {
				return !endIsUpToDate || !startIsUpToDate || firstPartContainsDestroyedNodes;
			}
		}

		/// <summary>
		/// Number of parts in the path.
		/// A part is either a sequence of adjacent nodes, or an off-mesh link.
		/// </summary>
		public readonly int partCount => parts != null ? parts.Length - firstPartIndex : 0;

		/// <summary>True if there is a path to follow</summary>
		public readonly bool hasPath => partCount > 0;

		/// <summary>Start point of the path</summary>
		public readonly Vector3 startPoint => this.parts[this.firstPartIndex].startPoint;

		/// <summary>
		/// End point of the path.
		///
		/// This is not necessarily the same as the destination, as this point may be clamped to the graph.
		/// </summary>
		public readonly Vector3 endPoint => this.parts[this.parts.Length - 1].endPoint;

		/// <summary>
		/// End point of the current path part.
		///
		/// If the path has multiple parts, this is typically the start of an off-mesh link.
		/// If the path has only one part, this is the same as <see cref="endPoint"/>.
		/// </summary>
		public readonly Vector3 endPointOfFirstPart => this.parts[this.firstPartIndex].endPoint;

		/// <summary>
		/// The minimum number of corners to request from GetNextCornerIndices to ensure the path can be simplified well.
		///
		/// The path simplification algorithm requires at least 2 corners on navmesh graphs, but 3 corners on grid graphs.
		/// </summary>
		public int desiredCornersForGoodSimplification => partGraphType == PartGraphType.Grid ? 3 : 2;

		/// <summary>
		/// True if the next part in the path exists, and is a valid link.
		/// This is true if the path has at least 2 parts and the second part is an off-mesh link.
		///
		/// If any nodes in the second part have been destroyed, this will return false.
		/// </summary>
		public readonly bool isNextPartValidLink => partCount > 1 && GetPartType(1) == Funnel.PartType.OffMeshLink && !PartContainsDestroyedNodes(1);

		/// <summary>Create a new empty path tracer</summary>
		public PathTracer(Allocator allocator) : this()
        {
        }

        /// <summary>Disposes of all unmanaged memory allocated by this path tracer and resets all properties</summary>
        public void Dispose()
        {
        }

        public enum RepairQuality
        {
            Low,
            High
        }

        /// <summary>
        /// Update the start point of the path, clamping it to the graph, and repairing the path if necessary.
        ///
        /// This may cause <see cref="isStale"/> to become true, if the path could not be repaired successfully.
        ///
        /// Returns: The new start point, which has been clamped to the graph.
        ///
        /// See: <see cref="UpdateEnd"/>
        /// </summary>
        public Vector3 UpdateStart(Vector3 position, RepairQuality quality, NativeMovementPlane movementPlane, ITraversalProvider traversalProvider, Path path)
        {
            return default;
        }

        /// <summary>
        /// Update the end point of the path, clamping it to the graph, and repairing the path if necessary.
        ///
        /// This may cause <see cref="isStale"/> to become true, if the path could not be repaired successfully.
        ///
        /// Returns: The new end point, which has been clamped to the graph.
        ///
        /// See: <see cref="UpdateEnd"/>
        /// </summary>
        public Vector3 UpdateEnd(Vector3 position, RepairQuality quality, NativeMovementPlane movementPlane, ITraversalProvider traversalProvider, Path path)
        {
            return default;
        }

        void AppendNode(bool toStart, GraphNode node)
        {
        }

        /// <summary>Appends the given nodes to the start or to the end of the path, one by one</summary>
        void AppendPath(bool toStart, CircularBuffer<GraphNode> path)
        {
        }

        /// <summary>
        /// Checks that invariants are satisfied.
        /// This is only called in the editor for performance reasons.
        ///
        /// - <see cref="firstPartIndex"/> must be in bounds of <see cref="parts"/>.
        /// - The first part must contain at least 1 node (unless there are no parts in the path at all).
        /// - The number of nodes in the first part must be equal to the number of portals in the funnel state + 1.
        /// - The number of portals in the funnel state must equal <see cref="portalIsNotInnerCorner.Length"/>.
        /// - The last node of the last part must end at the end of the path.
        /// - The first node of the first part must start at the start of the path.
        /// - <see cref="firstPartContainsDestroyedNodes"/> implies that there must be at least one destroyed node in the first part (this is an implication, not an equivalence).
        /// - If the first node is not destroyed, then <see cref="startNode"/> must be the first node in the first part.
        /// </summary>
        [System.Diagnostics.Conditional("UNITY_EDITOR")]
        void CheckInvariants()
        {
        }

        /// <summary>
        /// Removes nodes [startIndex, startIndex+toRemove) and then inserts the given nodes at startIndex.
        ///
        /// Returns true if the splicing succeeded.
        /// Returns false if splicing failed because it would have to access destroyed nodes.
        /// In that case the path is left unmodified.
        /// </summary>
        /// <param name="startIndex">Absolute index of the first node to remove</param>
        /// <param name="toRemove">Number of nodes to remove</param>
        /// <param name="toInsert">Nodes to insert at startIndex. The nodes must not be destroyed. Passing null is equivalent to an empty list.</param>
        bool SplicePath(int startIndex, int toRemove, List<GraphNode> toInsert)
        {
            return default;
        }

        static bool ContainsPoint(GraphNode node, Vector3 point, NativeMovementPlane plane)
        {
            return default;
        }

        /// <summary>
        /// Burstified function which checks if a point is inside a triangle-node and if so, projects that point onto the node's surface.
        /// Returns: If the point is inside the node.
        /// </summary>
        [BurstCompile]
        static bool ContainsAndProject(ref Int3 a, ref Int3 b, ref Int3 c, ref Vector3 p, float height, ref NativeMovementPlane movementPlane, out Vector3 projected)
        {
            projected = default(Vector3);
            return default;
        }

        static float3 ProjectOnSurface(float3 a, float3 b, float3 c, float3 p, float3 up)
        {
            return default;
        }

        void Repair(Vector3 point, bool isStart, RepairQuality quality, NativeMovementPlane movementPlane, ITraversalProvider traversalProvider, Path path, bool allowCache = true)
        {
        }

        private static readonly ProfilerMarker MarkerContains = new ProfilerMarker("ContainsNode");
        private static readonly ProfilerMarker MarkerClosest = new ProfilerMarker("ClosestPointOnNode");
        private static readonly ProfilerMarker MarkerGetNearest = new ProfilerMarker("GetNearest");
        const int NODES_TO_CHECK_FOR_DESTRUCTION = 5;

        /// <summary>
        /// Use a heuristic to determine when an agent has passed a portal and we need to pop it.
        ///
        /// Assumes the start point/end point of the first part is point, and simplifies the funnel
        /// accordingly. It uses the cached portals to determine if the agent has passed a portal.
        /// This works even if nodes have been destroyed.
        ///
        /// Note: Does not update the start/end point of the first part.
        /// </summary>
        void HeuristicallyPopPortals(bool isStartOfPart, Vector3 point)
        {
        }

        [System.Diagnostics.Conditional("UNITY_ASSERTIONS")]
        void AssertValidInPath(int absoluteNodeIndex)
        {
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        [IgnoredByDeepProfiler]
        readonly bool ValidInPath(int absoluteNodeIndex)
        {
            return default;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        [IgnoredByDeepProfiler]
        static bool Valid(GraphNode node) => !node.Destroyed && node.Walkable;

        /// <summary>
        /// Returns a hash with the most relevant information about a node.
        ///
        /// See: <see cref="nodeHashes"/>
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static int HashNode(GraphNode node)
        {
            return default;
        }

        void RepairFull(Vector3 point, bool isStart, RepairQuality quality, NativeMovementPlane movementPlane, ITraversalProvider traversalProvider, Path path)
        {
        }

        /// <summary>Calculates the squared distance from a point to the closest point on the node.</summary>
        /// <param name="node">The node to calculate the distance to</param>
        /// <param name="point">The point to calculate the distance from. For navmesh/recast/grid graphs this point should be in graph space.</param>
        /// <param name="projectionParams">Parameters for the projection, if the node is a triangle mesh node. The projection should be based on the node's graph.</param>
        static float SquaredDistanceToNode(GraphNode node, Vector3 point, ref BBTree.ProjectionParams projectionParams)
        {
            return default;
        }

        struct QueueItem
        {
            public GraphNode node;
            public int parent;
            public float distance;
        }

        static bool QueueHasNode(QueueItem[] queue, int count, GraphNode node)
        {
            return default;
        }

#if UNITY_2022_3_OR_NEWER
        static readonly QueueItem[][] TempQueues = new QueueItem[JobsUtility.ThreadIndexCount][];
        static readonly List<GraphNode>[] TempConnectionLists = new List<GraphNode>[JobsUtility.ThreadIndexCount];

        void GetTempQueue(out QueueItem[] queue, out List<GraphNode> connections)
        {
            queue = default(QueueItem[]);
            connections = default(List<GraphNode>);
        }
#else
		void GetTempQueue (out QueueItem[] queue, out List<GraphNode> connections) {
			queue = new QueueItem[16];
			connections = new List<GraphNode>();
		}
#endif

        /// <summary>
        /// Searches from currentNode until it finds a node that contains the given point.
        ///
        /// The return value is a list of nodes that start with currentNode and ends with the node that contains the given point, if it could be found.
        /// Otherwise, the return value will be an empty list.
        /// </summary>
        CircularBuffer<GraphNode> LocalSearch(GraphNode currentNode, Vector3 point, int maxNodesToSearch, NativeMovementPlane movementPlane, bool reverse, ITraversalProvider traversalProvider, Path path)
        {
            return default;
        }

        /// <summary>Renders the funnel for debugging purposes.</summary>
        /// <param name="draw">The command builder to use for drawing.</param>
        /// <param name="movementPlane">The movement plane of the agent.</param>
        public void DrawFunnel (CommandBuilder draw, NativeMovementPlane movementPlane) {
        }

        static Int3 MaybeSetYZero(Int3 p, bool setYToZero)
        {
            return default;
        }

        static bool IsInnerVertex(CircularBuffer<GraphNode> nodes, Funnel.PathPart part, int portalIndex, bool rightSide, List<GraphNode> alternativeNodes, NNConstraint nnConstraint, out int startIndex, out int endIndex, ITraversalProvider traversalProvider, Path path)
        {
            startIndex = default(int);
            endIndex = default(int);
            return default;
        }

        static bool IsInnerVertexTriangleMesh(CircularBuffer<GraphNode> nodes, Funnel.PathPart part, int portalIndex, bool rightSide, List<GraphNode> alternativeNodes, NNConstraint nnConstraint, out int startIndex, out int endIndex, ITraversalProvider traversalProvider, Path path)
        {
            startIndex = default(int);
            endIndex = default(int);
            return default;
        }

        bool FirstInnerVertex(NativeArray<int> indices, int numCorners, List<GraphNode> alternativePath, out int alternativeStartIndex, out int alternativeEndIndex, ITraversalProvider traversalProvider, Path path)
        {
            alternativeStartIndex = default(int);
            alternativeEndIndex = default(int);
            return default;
        }

        /// <summary>
        /// Estimates the remaining distance to the end of the current path part.
        ///
        /// Note: This method may modify the internal PathTracer state, so it is not safe to call it from multiple threads at the same time.
        /// </summary>
        public float EstimateRemainingPath(int maxCorners, ref NativeMovementPlane movementPlane)
        {
            return default;
        }

        [BurstCompile]
        static float EstimateRemainingPath(ref Funnel.FunnelState funnelState, ref Funnel.PathPart part, int maxCorners, ref NativeMovementPlane movementPlane)
        {
            return default;
        }

        [System.ThreadStatic]
        private static List<GraphNode> scratchList;

        /// <summary>
        /// Calculate the next corners in the path.
        ///
        /// This will also do additional simplifications to the path if possible. Inner corners will be removed.
        /// There is a limit to how many simplifications will be done per frame.
        ///
        /// If the path contains destroyed nodes, then <see cref="isStale"/> will become true and a best-effort result will be returned.
        ///
        /// Note: This method may modify the PathTracer state, so it is not safe to call it from multiple threads at the same time.
        /// </summary>
        /// <param name="buffer">The buffer to store the corners in. The first corner will be the start point.</param>
        /// <param name="maxCorners">The maximum number of corners to store in the buffer. At least 2 corners will always be stored.</param>
        /// <param name="scratchArray">A temporary array to use for calculations. This array will be resized if it is too small or uninitialized.</param>
        /// <param name="allocator">The allocator to use for the scratchArray, if it needs to be reallocated.</param>
        /// <param name="traversalProvider">The traversal provider to use for the path. Or null to use the default traversal provider.</param>
        /// <param name="path">The path to pass to the traversal provider. Or null.</param>
        public void GetNextCorners(NativeList<float3> buffer, int maxCorners, ref NativeArray<int> scratchArray, Allocator allocator, ITraversalProvider traversalProvider, Path path)
        {
        }

        /// <summary>
        /// Calculate the indices of the next corners in the path.
        ///
        /// This is like <see cref="GetNextCorners"/>, except that it returns indices referring to the internal <see cref="funnelState"/>.
        /// You can use <see cref="ConvertCornerIndicesToPathProjected"/> or <see cref="funnelState.ConvertCornerIndicesToPath"/> to convert the indices to world space positions.
        /// </summary>
        public int GetNextCornerIndices(ref NativeArray<int> buffer, int maxCorners, Allocator allocator, out bool lastCorner, ITraversalProvider traversalProvider, Path path)
        {
            lastCorner = default(bool);
            return default;
        }

        /// <summary>
        /// Converts corner indices to world space positions.
        ///
        /// The corners will not necessarily be in the same world space position as the real corners. Instead the path will be unwrapped and flattened,
        /// and then transformed onto a plane that lines up with the first portal in the funnel. For most 2D and 3D worlds, this distinction is irrelevant,
        /// but for curved worlds (e.g. a spherical world) this can make a big difference. In particular, steering towards unwrapped corners
        /// is much more likely to work well than steering towards the real corners, as they can be e.g. on the other side of a round planet.
        /// </summary>
        /// <param name="cornerIndices">The corner indices to convert. You can get these from #GetNextCornerIndices.</param>
        /// <param name="numCorners">The number of indices in the cornerIndices array.</param>
        /// <param name="lastCorner">True if the last corner in the path has been reached.</param>
        /// <param name="buffer">The buffer to store the converted positions in.</param>
        /// <param name="up">The up axis of the agent's movement plane.</param>
        public void ConvertCornerIndicesToPathProjected (NativeArray<int> cornerIndices, int numCorners, bool lastCorner, NativeList<float3> buffer, float3 up) {
        }

        /// <summary>
        /// Calculates a lower bound on the remaining distance to the end of the path part.
        ///
        /// It assumes the agent will follow the path, and then move in a straight line to the end of the path part.
        /// </summary>
        [BurstCompile]
        public static float RemainingDistanceLowerBound(in UnsafeSpan<float3> nextCorners, in float3 endOfPart, in NativeMovementPlane movementPlane)
        {
            return default;
        }

        /// <summary>
        /// Remove the first count parts of the path.
        ///
        /// This is used when an agent has traversed an off-mesh-link, and we want to start following the path after the off-mesh-link.
        /// </summary>
        /// <param name="count">The number of parts to remove.</param>
        /// <param name="traversalProvider">The traversal provider to use for the path. Or null to use the default traversal provider.</param>
        /// <param name="path">The path to pass to the traversal provider. Or null.</param>
        public void PopParts (int count, ITraversalProvider traversalProvider, Path path) {
        }

        public void RemoveAllButFirstNode(NativeMovementPlane movementPlane, ITraversalProvider traversalProvider)
        {
        }

        void RemoveAllPartsExceptFirst()
        {
        }

        /// <summary>Indicates if the given path part is a regular path part or an off-mesh link.</summary>
        /// <param name="partIndex">The index of the path part. Zero is the always the current path part.</param>
        public readonly Funnel.PartType GetPartType(int partIndex = 0)
        {
            return default;
        }

        public readonly bool PartContainsDestroyedNodes(int partIndex = 0)
        {
            return default;
        }

        public OffMeshLinks.OffMeshLinkTracer GetLinkInfo(int partIndex = 0)
        {
            return default;
        }

        void SetFunnelState(Funnel.PathPart part)
        {
        }

        void CalculateFunnelPortals (int startNodeIndex, int endNodeIndex, List<float3> outLeftPortals, List<float3> outRightPortals)
        {
        }

        /// <summary>Replaces the current path with a single node</summary>
        public void SetFromSingleNode (GraphNode node, Vector3 position, NativeMovementPlane movementPlane, PathRequestSettings pathfindingSettings) {
        }

        /// <summary>Clears the current path</summary>
        public void Clear () {
        }

        static int2 ResolveNormalizedGridPoint (GridGraph grid, ref CircularBuffer<GraphNode> nodes, UnsafeSpan<int> cornerIndices, Funnel.PathPart part, int index, out int nodeIndex) {
            nodeIndex = default(int);
            return default;
        }

        static int[] SplittingCoefficients = new int[] {
            0, 1,
            1, 2,
            1, 4,
            3, 4,
            1, 8,
            3, 8,
            5, 8,
            7, 8,
        };

        private static readonly ProfilerMarker MarkerSimplify = new ProfilerMarker("Simplify");

        static bool SimplifyGridInnerVertex(ref CircularBuffer<GraphNode> nodes, UnsafeSpan<int> cornerIndices, Funnel.PathPart part, ref CircularBuffer<byte> portalIsNotInnerCorner, List<GraphNode> alternativePath, out int alternativeStartIndex, out int alternativeEndIndex, NNConstraint nnConstraint, ITraversalProvider traversalProvider, Path path, bool lastCorner)
        {
            alternativeStartIndex = default(int);
            alternativeEndIndex = default(int);
            return default;
        }

        /// <summary>
        /// Removes diagonal connections in a grid path and replaces them with two axis-aligned connections.
        ///
        /// This is done to make the funnel algorithm work better on grid graphs.
        /// </summary>
        static void RemoveGridPathDiagonals(Funnel.PathPart[] parts, int partIndex, ref CircularBuffer<GraphNode> path, ref CircularBuffer<int> pathNodeHashes, NNConstraint nnConstraint, ITraversalProvider traversalProvider, Path pathObject)
        {
        }

        static PartGraphType PartGraphTypeFromNode(GraphNode node)
        {
            return default;
        }

        /// <summary>Replaces the current path with the given path.</summary>
        /// <param name="path">The path to follow.</param>
        /// <param name="movementPlane">The movement plane of the agent.</param>
        public void SetPath(ABPath path, NativeMovementPlane movementPlane)
        {
        }

        /// <summary>Replaces the current path with the given path.</summary>
        /// <param name="parts">The individual parts of the path. See \reflink{Funnel.SplitIntoParts}.</param>
        /// <param name="nodes">All nodes in the path. The path parts refer to slices of this array.</param>
        /// <param name="unclampedStartPoint">The start point of the path. This is typically the start point that was passed to the path request, or the agent's current position.</param>
        /// <param name="unclampedEndPoint">The end point of the path. This is typically the destination point that was passed to the path request.</param>
        /// <param name="movementPlane">The movement plane of the agent.</param>
        /// <param name="pathfindingSettings">Pathfinding settings that the path was calculated with. You may pass PathRequestSettings.Default if you don't use tags, traversal providers, or multiple graphs.</param>
        /// <param name="path">The path to pass to the traversal provider. Or null.</param>
        public void SetPath(List<Funnel.PathPart> parts, List<GraphNode> nodes, Vector3 unclampedStartPoint, Vector3 unclampedEndPoint, NativeMovementPlane movementPlane, PathRequestSettings pathfindingSettings, Path path)
        {
        }

        /// <summary>Returns a deep clone of this object</summary>
        public PathTracer Clone()
        {
            return default;
        }
    }
}
