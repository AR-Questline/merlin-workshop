using System.Collections.Generic;
using Unity.Collections;
using UnityEngine;
using UnityEngine.Profiling;
using Pathfinding.Pooling;
using Pathfinding.Collections;
using System;
using UnityEngine.Assertions;

namespace Pathfinding {
	/// <summary>
	/// Manager for off-mesh links.
	///
	/// This manager tracks all active off-mesh links in the scene and recalculates them when needed.
	/// If an off-mesh link is activated, a <see cref="LinkGraph"/> will also be added to the graph list to store the special nodes necessary for the links to work.
	///
	/// Whenever a graph update happens, the <see cref="DirtyBounds"/> method should be called with the bounds of the updated area.
	/// This will cause the links touching that bounding box to be recalculated at the end of the graph update step.
	///
	/// Typically you will not need to interact with this class yourself, instead you can use the pre-built components like <see cref="NodeLink2"/>.
	/// </summary>
	public class OffMeshLinks {
		AABBTree<OffMeshLinkCombined> tree = new AABBTree<OffMeshLinkCombined>();
		List<OffMeshLinkSource> pendingAdd = new List<OffMeshLinkSource>();
		bool updateScheduled;
		AstarPath astar;

		public OffMeshLinks(AstarPath astar) {
        }

        /// <summary>
        /// The start or end point of an off-mesh link.
        ///
        /// See: <see cref="OffMeshLinkSource"/>
        /// </summary>
        public struct Anchor {
			/// <summary>Where the link connects to the navmesh</summary>
			public Vector3 center;
			/// <summary>Rotation that the character should align itself with when traversing the link</summary>
			public Quaternion rotation;
			/// <summary>
			/// Width of the link.
			///
			/// Note: No values other than 0 are currently supported.
			/// </summary>
			public float width;

			/// <summary>First point on the segment that makes up this anchor</summary>
			public readonly Vector3 point1 => center + rotation * new Vector3(-0.5f * width, 0, 0);

			/// <summary>Second point on the segment that makes up this anchor</summary>
			public readonly Vector3 point2 => center + rotation * new Vector3(0.5f * width, 0, 0);

			public static bool operator ==(Anchor a, Anchor b) => a.center == b.center && a.rotation == b.rotation && a.width == b.width;
			public static bool operator !=(Anchor a, Anchor b) => a.center != b.center || a.rotation != b.rotation || a.width != b.width;

			public override bool Equals(object obj) => obj is Anchor && this == (Anchor)obj;
			public override int GetHashCode() => (center.GetHashCode() * 23 ^ rotation.GetHashCode()) * 23 ^ width.GetHashCode();
		}

		/// <summary>Determines how a link is connected in the graph</summary>
		public enum Directionality {
			/// <summary>Movement is only allowed from the start point to the end point</summary>
			OneWay,
			/// <summary>Movement is allowed in both directions</summary>
			TwoWay,
		}

		[System.Flags]
		public enum OffMeshLinkStatus {
			Inactive = 1 << 0,
			Pending = 1 << 1,
			Active = 1 << 2,
			FailedToConnectStart = Inactive | 1 << 3,
			FailedToConnectEnd = Inactive | 1 << 4,
			PendingRemoval = 1 << 5,
		}

		/// <summary>
		/// Information about an off-mesh link.
		///
		/// Off-mesh links connect two points on the navmesh which are not necessarily connected by a normal navmesh connection.
		///
		/// See: <see cref="NodeLink2"/>
		/// See: <see cref="OffMeshLinks"/>
		/// </summary>
		public readonly struct OffMeshLinkTracer {
			public OffMeshLinkTracer(OffMeshLinkConcrete link, bool reversed) : this()
            {
            }

            public OffMeshLinkTracer(OffMeshLinkConcrete link, Vector3 relativeStart, Vector3 relativeEnd, bool isReverse) : this()
            {
            }

            /// <summary>
            /// The off-mesh link that the agent is traversing.
            ///
            /// Note: If the off-mesh link is destroyed while the agent is traversing it, properties like <see cref="OffMeshLinkConcrete.gameObject"/>, may refer to a destroyed gameObject.
            /// </summary>
            public readonly OffMeshLinkConcrete link;

			/// <summary>
			/// The start point of the off-mesh link from the agent's perspective.
			///
			/// This is the point where the agent starts traversing the off-mesh link, regardless of if the link is traversed from the start to end or from end to start.
			/// </summary>
			public readonly Vector3 relativeStart;

			/// <summary>
			/// The end point of the off-mesh link from the agent's perspective.
			///
			/// This is the point where the agent will finish traversing the off-mesh link, regardless of if the link is traversed from start to end or from end to start.
			/// </summary>
			public readonly Vector3 relativeEnd;

			/// <summary>
			/// True if the agent is traversing the off-mesh link from original link's end to its start point.
			///
			/// Note: The <see cref="relativeStart"/> and <see cref="relativeEnd"/> fields are always set from the agent's perspective. So the agent always moves from <see cref="relativeStart"/> to <see cref="relativeEnd"/>.
			/// </summary>
			public readonly bool isReverse;

			/// <summary>\copydocref{OffMeshLinkSource.component}</summary>
			public Component component => link.component;
			/// <summary>\copydocref{OffMeshLinkSource.gameObject}</summary>
			public GameObject gameObject => link.gameObject;
		}

		public class OffMeshLinkSource {
			/// <summary>The start of the link</summary>
			public Anchor start;

			/// <summary>The end of the link</summary>
			public Anchor end;
			public Directionality directionality;

			/// <summary>
			/// Tag to apply to this link.
			///
			/// See: tags (view in online documentation for working links)
			/// </summary>
			public PathfindingTag tag;

			/// <summary>Multiplies the cost of traversing this link by this amount</summary>
			public float costFactor; // TODO: Add constant cost?

			/// <summary>
			/// Maximum distance from the start/end points to the navmesh.
			///
			/// If the distance is greater than this, the link will not be connected to the navmesh.
			/// </summary>
			public float maxSnappingDistance;

			/// <summary>
			/// Graph mask for which graphs the link is allowed to connect to.
			///
			/// The link's endpoints will be connected to the closest valid node on any graph that matches the mask.
			/// </summary>
			public GraphMask graphMask;

			public IOffMeshLinkHandler handler;

			/// <summary>
			/// The Component associated with this link.
			///
			/// Typically this will be a <see cref="NodeLink2"/> component. But users can also create their own components and fill out this field as appropriate.
			///
			/// This field is not used for anything by the pathfinding system itself, it is only used to make it easier for users to find the component associated with a link.
			///
			/// Warning: If the link has been destroyed, this may return a destroyed component.
			/// A link may be destroyed even while a character is traversing it.
			/// </summary>
			public Component component;

			/// <summary>
			/// The GameObject associated with this link.
			///
			/// This field is not used for anything by the pathfinding system itself, it is only used to make it easier for users to find the GameObject associated with a link.
			///
			/// Warning: If the link has been destroyed, this may return a destroyed GameObject.
			/// A link may be destroyed even while a character is traversing it.
			/// </summary>
			public GameObject gameObject => component != null ? component.gameObject : null;

			internal AABBTree<OffMeshLinkCombined>.Key treeKey;

			public OffMeshLinkStatus status { get; internal set; } = OffMeshLinkStatus.Inactive;

			/// <summary>
			/// Bounding box which encapsulates the link and any position on the navmesh it could possibly be connected to.
			///
			/// This is used to determine which links need to be recalculated when a graph update happens.
			/// </summary>
			public Bounds bounds {
				get {
					var b = new Bounds();
					b.SetMinMax(start.point1, start.point2);
					b.Encapsulate(end.point1);
					b.Encapsulate(end.point2);
					b.Expand(maxSnappingDistance*2);
					return b;
				}
			}
		}

		internal class OffMeshLinkCombined {
			public OffMeshLinkSource source;
			public OffMeshLinkConcrete concrete;
		}

		public class OffMeshLinkConcrete {
			/// <summary>\copydocref{OffMeshLinkSource.start}</summary>
			public Anchor start;
			/// <summary>\copydocref{OffMeshLinkSource.end}</summary>
			public Anchor end;
			public GraphNode[] startNodes;
			public GraphNode[] endNodes;
			public LinkNode startLinkNode;
			public LinkNode endLinkNode;
			/// <summary>\copydocref{OffMeshLinkSource.directionality}</summary>
			public Directionality directionality;
			/// <summary>\copydocref{OffMeshLinkSource.tag}</summary>
			public PathfindingTag tag;
			public float costFactor;
			internal bool staleConnections;
			internal OffMeshLinkSource source;

			/// <summary>\copydocref{OffMeshLinkSource.handler}</summary>
			public IOffMeshLinkHandler handler => source.handler;

			/// <summary>\copydocref{OffMeshLinkSource.component}</summary>
			public Component component => source.component;

			/// <summary>\copydocref{OffMeshLinkSource.gameObject}</summary>
			public GameObject gameObject => source.component != null ? source.component.gameObject : null;

			// public Bounds bounds {
			// 	get {
			// 		var b = new Bounds();
			// 		b.SetMinMax(start.point1, start.point2);
			// 		b.Encapsulate(end.point1);
			// 		b.Encapsulate(end.point2);
			// 		return b;
			// 	}
			// }

			public bool Equivalent (OffMeshLinkConcrete other) {
                return default;
            }

            public void Disconnect()
            {
            }

            public void Connect(LinkGraph linkGraph, OffMeshLinkSource source)
            {
            }

            public OffMeshLinkTracer GetTracer(LinkNode firstNode)
            {
                return default;
            }
        }

		/// <summary>
		/// Get all graphs that this link is connected to.
		///
		/// Returns: A list of all graphs that this link is connected to. This does not include the link graph.
		/// An empty list will be returned if the link is not connected to any graphs.
		///
		/// Note: For lower GC pressure, the returned list should be pooled after you are done with it. See: pooling (view in online documentation for working links)
		/// </summary>
		/// <param name="link">The link to get connected graphs for.</param>
		public List<NavGraph> ConnectedGraphs (OffMeshLinkSource link) {
            return default;
        }

        /// <summary>
        /// Adds a new off-mesh link.
        ///
        /// If any graphs change in the future, the link will automatically be updated to connect to the updated graphs.
        ///
        /// Note: The link will not be added immediately, it will be added at the end of the current graph update step.
        /// Or, if no graph update is currently running, a graph update will be scheduled, and the link will be added at the end of that update.
        /// This is to avoid modifying the graph during a graph update.
        ///
        /// See: <see cref="Remove"/>
        /// </summary>
        /// <param name="link">The link to add.</param>
        public void Add(OffMeshLinkSource link)
        {
        }

        internal void OnDisable()
        {
        }

        /// <summary>
        /// Removes an existing off-mesh link.
        ///
        /// Note: The link will not be removed immediately, it will be removed at the end of the current graph update step.
        /// Or, if no graph update is currently running, a graph update will be scheduled, and the link will be removed at the end of that update.
        /// This is to avoid modifying the graph during a graph update.
        ///
        /// See: <see cref="Add"/>
        /// </summary>
        /// <param name="link">The link to remove. If the link is already removed, nothing will be done.</param>
        public void Remove(OffMeshLinkSource link)
        {
        }

        NNConstraint cachedNNConstraint = NNConstraint.Walkable;

        bool ClampSegment(Anchor anchor, GraphMask graphMask, float maxSnappingDistance, out Anchor result, List<GraphNode> nodes)
        {
            result = default(Anchor);
            return default;
        }

        /// <summary>
        /// Mark links touching the given bounds as dirty.
        ///
        /// The bounds should contain the surface of all nodes that have been changed.
        ///
        /// This will cause the links to be recalculated as soon as possible.
        ///
        /// Note: Since graphs should only be modified during graph updates, this method should also only be called during a graph update.
        /// </summary>
        public void DirtyBounds(Bounds bounds)
        {
        }

        /// <summary>
        /// Mark a link as dirty.
        ///
        /// This will cause the link to be recalculated as soon as possible.
        /// </summary>
        public void Dirty (OffMeshLinkSource link) {
        }

        internal void DirtyNoSchedule (OffMeshLinkSource link) {
        }

        void ScheduleUpdate () {
        }

        /// <summary>
        /// Get the nearest link to a point.
        ///
        /// Returns: The nearest link to the point or a default <see cref="OffMeshLinkTracer"/> if no link was found.
        /// The returned struct contains both the link and information about which side of the link is closest to the point.
        /// If the end is closer than the start, then a reversed <see cref="OffMeshLinkTracer"/> will be returned.
        /// </summary>
        /// <param name="point">Point to search around.</param>
        /// <param name="maxDistance">Maximum distance to search. Use a small distance for better performance.</param>
        public OffMeshLinkTracer GetNearest (Vector3 point, float maxDistance) {
            return default;
        }

        internal void Refresh()
        {
        }
    }
}
