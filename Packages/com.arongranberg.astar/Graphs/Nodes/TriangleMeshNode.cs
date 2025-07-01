#pragma warning disable 0162
using UnityEngine;
using Pathfinding.Serialization;
using Pathfinding.Collections;
using UnityEngine.Assertions;
using Unity.Mathematics;
using Pathfinding.Util;
using Unity.Burst;
using Unity.Profiling;

namespace Pathfinding {
	/// <summary>Interface for something that holds a triangle based navmesh</summary>
	public interface INavmeshHolder : ITransformedGraph {
		void GetNodes(System.Action<GraphNode> del);

		/// <summary>Position of vertex number i in the world</summary>
		Int3 GetVertex(int i);

		/// <summary>
		/// Position of vertex number i in coordinates local to the graph.
		/// The up direction is always the +Y axis for these coordinates.
		/// </summary>
		Int3 GetVertexInGraphSpace(int i);

		int GetVertexArrayIndex(int index);

		/// <summary>Transforms coordinates from graph space to world space</summary>
		void GetTileCoordinates(int tileIndex, out int x, out int z);
	}

	/// <summary>Node represented by a triangle</summary>
	[Unity.Burst.BurstCompile]
	// Sealing the class provides a nice performance boost (~5-10%) during pathfinding, because the JIT can inline more things and use non-virtual calls.
	public sealed class TriangleMeshNode : MeshNode {
		public TriangleMeshNode () {
        }

        public TriangleMeshNode (AstarPath astar) {
        }

        /// <summary>
        /// Legacy compatibility.
        /// Enabling this will make pathfinding use node centers, which leads to less accurate paths (but it's faster).
        /// </summary>
        public const bool InaccuratePathSearch = false;
		internal override int PathNodeVariants => InaccuratePathSearch ? 1 : 3;

		/// <summary>Internal vertex index for the first vertex</summary>
		public int v0;

		/// <summary>Internal vertex index for the second vertex</summary>
		public int v1;

		/// <summary>Internal vertex index for the third vertex</summary>
		public int v2;

		/// <summary>Used for synchronised access to the <see cref="_navmeshHolders"/> array</summary>
		static readonly System.Object lockObject = new System.Object();

		static INavmeshHolder[] _navmeshHolders {
			get => AstarPath.active._navmeshHolders;
			set => AstarPath.active._navmeshHolders = value;
		}

		[System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
		public static INavmeshHolder GetNavmeshHolder (uint graphIndex) {
            return default;
        }

        /// <summary>
        /// Tile index in the recast or navmesh graph that this node is part of.
        /// See: <see cref="NavmeshBase.GetTiles"/>
        /// </summary>
        public int TileIndex => (v0 >> NavmeshBase.TileIndexOffset) & NavmeshBase.TileIndexMask;

		/// <summary>
		/// Sets the internal navmesh holder for a given graph index.
		/// Warning: Internal method
		/// </summary>
		public static void SetNavmeshHolder (int graphIndex, INavmeshHolder graph) {
        }

        public static void ClearNavmeshHolder (int graphIndex, INavmeshHolder graph) {
        }

        /// <summary>Set the position of this node to the average of its 3 vertices</summary>
        public void UpdatePositionFromVertices () {
        }

        /// <summary>
        /// Return a number identifying a vertex.
        /// This number does not necessarily need to be a index in an array but two different vertices (in the same graph) should
        /// not have the same vertex numbers.
        /// </summary>
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
		[IgnoredByDeepProfiler]
		public int GetVertexIndex (int i) {
            return default;
        }

        /// <summary>
        /// Return a number specifying an index in the source vertex array.
        /// The vertex array can for example be contained in a recast tile, or be a navmesh graph, that is graph dependant.
        /// This is slower than GetVertexIndex, if you only need to compare vertices, use GetVertexIndex.
        /// </summary>
        public int GetVertexArrayIndex (int i) {
            return default;
        }

        /// <summary>Returns all 3 vertices of this node in world space</summary>
        public void GetVertices (out Int3 v0, out Int3 v1, out Int3 v2) {
            v0 = default(Int3);
            v1 = default(Int3);
            v2 = default(Int3);
        }

        /// <summary>Returns all 3 vertices of this node in graph space</summary>
        public void GetVerticesInGraphSpace (out Int3 v0, out Int3 v1, out Int3 v2) {
            v0 = default(Int3);
            v1 = default(Int3);
            v2 = default(Int3);
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
		[IgnoredByDeepProfiler]
		public override Int3 GetVertex (int i) {
            return default;
        }

        public Int3 GetVertexInGraphSpace (int i) {
            return default;
        }

        public override int GetVertexCount () {
            return default;
        }

        /// <summary>
        /// Projects the given point onto the plane of this node's surface.
        ///
        /// The point will be projected down to a plane that contains the surface of the node.
        /// If the point is not contained inside the node, it is projected down onto this plane anyway.
        /// </summary>
        public Vector3 ProjectOnSurface (Vector3 point) {
            return default;
        }

        public override Vector3 ClosestPointOnNode (Vector3 p) {
            return default;
        }

        /// <summary>
        /// Closest point on the node when seen from above.
        /// This method is mostly for internal use as the <see cref="Pathfinding.NavmeshBase.Linecast"/> methods use it.
        ///
        /// - The returned point is the closest one on the node to p when seen from above (relative to the graph).
        ///   This is important mostly for sloped surfaces.
        /// - The returned point is an Int3 point in graph space.
        /// - It is guaranteed to be inside the node, so if you call <see cref="ContainsPointInGraphSpace"/> with the return value from this method the result is guaranteed to be true.
        ///
        /// This method is slower than e.g <see cref="ClosestPointOnNode"/> or <see cref="ClosestPointOnNodeXZ"/>.
        /// However they do not have the same guarantees as this method has.
        /// </summary>
        internal Int3 ClosestPointOnNodeXZInGraphSpace(Vector3 p)
        {
            return default;
        }

        public override Vector3 ClosestPointOnNodeXZ(Vector3 p)
        {
            return default;
        }

        /// <summary>
        /// Checks if point is inside the node when seen from above.
        ///
        /// Note that <see cref="ContainsPointInGraphSpace"/> is faster than this method as it avoids
        /// some coordinate transformations. If you are repeatedly calling this method
        /// on many different nodes but with the same point then you should consider
        /// transforming the point first and then calling ContainsPointInGraphSpace.
        ///
        /// <code>
        /// Int3 p = (Int3)graph.transform.InverseTransform(point);
        ///
        /// node.ContainsPointInGraphSpace(p);
        /// </code>
        /// </summary>
        public override bool ContainsPoint(Vector3 p)
        {
            return default;
        }

        /// <summary>Checks if point is inside the node when seen from above, as defined by the movement plane</summary>
        public bool ContainsPoint(Vector3 p, NativeMovementPlane movementPlane)
        {
            return default;
        }

        /// <summary>
        /// Checks if point is inside the node in graph space.
        ///
        /// In graph space the up direction is always the Y axis so in principle
        /// we project the triangle down on the XZ plane and check if the point is inside the 2D triangle there.
        /// </summary>
        public override bool ContainsPointInGraphSpace(Int3 p)
        {
            return default;
        }

        public static readonly Unity.Profiling.ProfilerMarker MarkerDecode = new Unity.Profiling.ProfilerMarker("Decode");
		public static readonly Unity.Profiling.ProfilerMarker MarkerGetVertices = new Unity.Profiling.ProfilerMarker("GetVertex");
		public static readonly Unity.Profiling.ProfilerMarker MarkerClosest = new Unity.Profiling.ProfilerMarker("MarkerClosest");

		public override Int3 DecodeVariantPosition (uint pathNodeIndex, uint fractionAlongEdge) {
            return default;
        }

        [BurstCompile(FloatMode = FloatMode.Fast)]
		static void InterpolateEdge (ref Int3 p1, ref Int3 p2, uint fractionAlongEdge, out Int3 pos) {
            pos = default(Int3);
        }

        public override void OpenAtPoint (Path path, uint pathNodeIndex, Int3 point, uint gScore) {
        }

        public override void Open (Path path, uint pathNodeIndex, uint gScore) {
        }

        void OpenAtPoint (Path path, uint pathNodeIndex, Int3 pos, int edge, uint gScore) {
        }

        void OpenSingleEdge (Path path, uint pathNodeIndex, TriangleMeshNode other, int sharedEdgeOnOtherNode, Int3 pos, uint gScore) {
        }

        [Unity.Burst.BurstCompile]
        static void OpenSingleEdgeBurst(ref Int3 s1, ref Int3 s2, ref Int3 pos, ushort pathID, uint pathNodeIndex, uint candidatePathNodeIndex, uint candidateNodeIndex, uint candidateG, ref UnsafeSpan<PathNode> pathNodes, ref BinaryHeap heap, ref HeuristicObjective heuristicObjective)
        {
        }

        [Unity.Burst.BurstCompile]
		static void CalculateBestEdgePosition (ref Int3 s1, ref Int3 s2, ref Int3 pos, out int3 closestPointAlongEdge, out uint quantizedFractionAlongEdge, out uint cost) {
            closestPointAlongEdge = default(int3);
            quantizedFractionAlongEdge = default(uint);
            cost = default(uint);
        }

        /// <summary>
        /// Returns the edge which is shared with other.
        ///
        /// If there is no shared edge between the two nodes, then -1 is returned.
        ///
        /// The vertices in the edge can be retrieved using
        /// <code>
        /// var edge = node.SharedEdge(other);
        /// var a = node.GetVertex(edge);
        /// var b = node.GetVertex((edge+1) % node.GetVertexCount());
        /// </code>
        ///
        /// See: <see cref="GetPortal"/> which also handles edges that are shared over tile borders and some types of node links
        /// </summary>
        public int SharedEdge(GraphNode other)
        {
            return default;
        }

        public override bool GetPortal(GraphNode toNode, out Vector3 left, out Vector3 right)
        {
            left = default(Vector3);
            right = default(Vector3);
            return default;
        }

        public bool GetPortalInGraphSpace(TriangleMeshNode toNode, out Int3 a, out Int3 b, out int aIndex, out int bIndex)
        {
            a = default(Int3);
            b = default(Int3);
            aIndex = default(int);
            bIndex = default(int);
            return default;
        }

        public bool GetPortal(GraphNode toNode, out Vector3 left, out Vector3 right, out int aIndex, out int bIndex)
        {
            left = default(Vector3);
            right = default(Vector3);
            aIndex = default(int);
            bIndex = default(int);
            return default;
        }

        /// <summary>TODO: This is the area in XZ space, use full 3D space for higher correctness maybe?</summary>
        public override float SurfaceArea()
        {
            return default;
        }

        public override Vector3 RandomPointOnSurface()
        {
            return default;
        }

        public override void SerializeNode(GraphSerializationContext ctx)
        {
        }

        public override void DeserializeNode(GraphSerializationContext ctx)
        {
        }

        // Destroy node without cleanup. Use when disposing all graphs with AstarPath.OnDisable
        public void DestroyFast() {
        }
    }
}
