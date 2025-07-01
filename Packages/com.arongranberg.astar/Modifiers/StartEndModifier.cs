using UnityEngine;
using System.Collections.Generic;

namespace Pathfinding {
	[System.Serializable]
	/// <summary>
	/// Adjusts start and end points of a path.
	///
	/// This modifier is included in the <see cref="Pathfinding.Seeker"/> component and is always used if you are using a Seeker.
	/// When a path is calculated the resulting path will only be the positions of the nodes it passes through.
	/// However often you may not want to navigate to the center of a specific node but instead to a point on the surface of a node.
	/// This modifier will adjust the endpoints of the path.
	///
	/// [Open online documentation to see images]
	/// </summary>
	public class StartEndModifier : PathModifier {
		public override int Order { get { return 0; } }

		/// <summary>
		/// Add points to the path instead of replacing them.
		/// If for example <see cref="exactEndPoint"/> is set to ClosestOnNode then the path will be modified so that
		/// the path goes first to the center of the last node in the path and then goes to the closest point
		/// on the node to the end point in the path request.
		///
		/// If this is false however then the relevant points in the path will simply be replaced.
		/// In the above example the path would go directly to the closest point on the node without passing
		/// through the center of the node.
		/// </summary>
		public bool addPoints;

		/// <summary>
		/// How the start point of the path will be determined.
		/// See: <see cref="Exactness"/>
		/// </summary>
		public Exactness exactStartPoint = Exactness.ClosestOnNode;

		/// <summary>
		/// How the end point of the path will be determined.
		/// See: <see cref="Exactness"/>
		/// </summary>
		public Exactness exactEndPoint = Exactness.ClosestOnNode;

		/// <summary>
		/// Will be called when a path is processed.
		/// The value which is returned will be used as the start point of the path
		/// and potentially clamped depending on the value of the <see cref="exactStartPoint"/> field.
		/// Only used for the Original, Interpolate and NodeConnection modes.
		/// </summary>
		public System.Func<Vector3> adjustStartPoint;

		/// <summary>
		/// Sets where the start and end points of a path should be placed.
		///
		/// Here is a legend showing what the different items in the above images represent.
		/// The images above show a path coming in from the top left corner and ending at a node next to an obstacle as well as 2 different possible end points of the path and how they would be modified.
		/// [Open online documentation to see images]
		/// </summary>
		public enum Exactness {
			/// <summary>
			/// The point is snapped to the position of the first/last node in the path.
			/// Use this if your game is very tile based and you want your agents to stop precisely at the center of the nodes.
			/// If you recalculate the path while the agent is moving you may want the start point snapping to be ClosestOnNode and the end point snapping to be SnapToNode however
			/// as while an agent is moving it will likely not be right at the center of a node.
			///
			/// [Open online documentation to see images]
			/// </summary>
			SnapToNode,
			/// <summary>
			/// The point is set to the exact point which was passed when creating the path request.
			/// Note that if a path was for example requested to a point inside an obstacle, then the last point of the path will be inside that obstacle, which is usually not what you want.
			/// Consider using the <see cref="Exactness.ClosestOnNode"/> option instead.
			///
			/// [Open online documentation to see images]
			/// </summary>
			Original,
			/// <summary>
			/// The point is set to the closest point on the line between either the two first points or the two last points.
			/// Usually you will want to use the NodeConnection mode instead since that is usually the behaviour that you really want.
			/// This mode exists mostly for compatibility reasons.
			/// [Open online documentation to see images]
			/// Deprecated: Use NodeConnection instead.
			/// </summary>
			Interpolate,
			/// <summary>
			/// The point is set to the closest point on the surface of the node. Note that some node types (point nodes) do not have a surface, so the "closest point" is simply the node's position which makes this identical to <see cref="Exactness.SnapToNode"/>.
			/// This is the mode that you almost always want to use in a free movement 3D world.
			/// [Open online documentation to see images]
			/// </summary>
			ClosestOnNode,
			/// <summary>
			/// The point is set to the closest point on one of the connections from the start/end node.
			/// This mode may be useful in a grid based or point graph based world when using the AILerp script.
			///
			/// Note: If you are using this mode with a <see cref="Pathfinding.PointGraph"/> you probably also want to use the <see cref="Pathfinding.PointGraph.NodeDistanceMode Connection"/> for <see cref="PointGraph.nearestNodeDistanceMode"/>.
			///
			/// [Open online documentation to see images]
			/// </summary>
			NodeConnection,
		}

		/// <summary>
		/// Do a straight line check from the node's center to the point determined by the <see cref="Exactness"/>.
		/// There are very few cases where you will want to use this. It is mostly here for
		/// backwards compatibility reasons.
		///
		/// Version: Since 4.1 this field only has an effect for the <see cref="Exactness"/> mode Original because that's the only one where it makes sense.
		/// </summary>
		public bool useRaycasting;
		public LayerMask mask = -1;

		/// <summary>
		/// Do a straight line check from the node's center to the point determined by the <see cref="Exactness"/>.
		/// See: <see cref="useRaycasting"/>
		///
		/// Version: Since 4.1 this field only has an effect for the <see cref="Exactness"/> mode Original because that's the only one where it makes sense.
		/// </summary>
		public bool useGraphRaycasting;

		List<GraphNode> connectionBuffer;
		System.Action<GraphNode> connectionBufferAddDelegate;

		public override void Apply (Path _p) {
        }

        Vector3 Snap(ABPath path, Exactness mode, bool start, out bool forceAddPoint, out int closestConnectionIndex)
        {
            forceAddPoint = default(bool);
            closestConnectionIndex = default(int);
            return default;
        }

        protected Vector3 GetClampedPoint(Vector3 from, Vector3 to, GraphNode hint)
        {
            return default;
        }
    }
}
