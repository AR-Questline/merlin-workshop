using UnityEngine;

namespace Pathfinding.Graphs.Navmesh {
	using Pathfinding.Drawing;
	using Pathfinding.Util;

	/// <summary>
	/// Pruning of recast navmesh regions.
	/// A RelevantGraphSurface component placed in the scene specifies that
	/// the navmesh region it is inside should be included in the navmesh.
	///
	/// See: Pathfinding.RecastGraph.relevantGraphSurfaceMode
	/// </summary>
	[AddComponentMenu("Pathfinding/Navmesh/RelevantGraphSurface")]
	[HelpURL("https://arongranberg.com/astar/documentation/stable/relevantgraphsurface.html")]
	public class RelevantGraphSurface : VersionedMonoBehaviour {
		private static RelevantGraphSurface root;

		public float maxRange = 1;

		private RelevantGraphSurface prev;
		private RelevantGraphSurface next;
		private Vector3 position;

		public Vector3 Position {
			get { return position; }
		}

		public RelevantGraphSurface Next {
			get { return next; }
		}

		public RelevantGraphSurface Prev {
			get { return prev; }
		}

		public static RelevantGraphSurface Root {
			get { return root; }
		}

		public void UpdatePosition () {
        }

        void OnEnable () {
        }

        void OnDisable () {
        }

        /// <summary>
        /// Updates the positions of all relevant graph surface components.
        /// Required to be able to use the position property reliably.
        /// </summary>
        public static void UpdateAllPositions()
        {
        }

        public static void FindAllGraphSurfaces () {
        }

        public override void DrawGizmos () {
        }
    }
}
