using UnityEngine;
using Pathfinding.Pooling;

namespace Pathfinding {
	/// <summary>
	/// Restrict suitable nodes by if they have been searched by a FloodPath.
	///
	/// Suitable nodes are in addition to the basic contraints, only the nodes which return true on a FloodPath.HasPathTo (node) call.
	/// See: Pathfinding.FloodPath
	/// See: Pathfinding.FloodPathTracer
	/// </summary>
	public class FloodPathConstraint : NNConstraint {
		readonly FloodPath path;

		public FloodPathConstraint (FloodPath path) {
        }

        public override bool Suitable(GraphNode node)
        {
            return default;
        }
    }

	/// <summary>
	/// Traces a path created with the Pathfinding.FloodPath.
	///
	/// See Pathfinding.FloodPath for examples on how to use this path type
	///
	/// [Open online documentation to see images]
	/// </summary>
	public class FloodPathTracer : ABPath {
		/// <summary>Reference to the FloodPath which searched the path originally</summary>
		protected FloodPath flood;

		protected override bool hasEndPoint => false;

		/// <summary>
		/// Default constructor.
		/// Do not use this. Instead use the static Construct method which can handle path pooling.
		/// </summary>
		public FloodPathTracer () {}

        public static FloodPathTracer Construct(Vector3 start, FloodPath flood, OnPathDelegate callback = null)
        {
            return default;
        }

        protected void Setup(Vector3 start, FloodPath flood, OnPathDelegate callback)
        {
        }

        protected override void Reset()
        {
        }

        /// <summary>
        /// Initializes the path.
        /// Traces the path from the start node.
        /// </summary>
        protected override void Prepare()
        {
        }

        protected override void CalculateStep(long targetTick)
        {
        }

        /// <summary>
        /// Traces the calculated path from the start node to the end.
        /// This will build an array (<see cref="path)"/> of the nodes this path will pass through and also set the <see cref="vectorPath"/> array to the <see cref="path"/> arrays positions.
        /// This implementation will use the <see cref="flood"/> (FloodPath) to trace the path from precalculated data.
        /// </summary>
        protected override void Trace(uint fromPathNodeIndex)
        {
        }
    }
}
