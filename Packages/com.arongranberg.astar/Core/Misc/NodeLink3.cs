using UnityEngine;
using System.Collections.Generic;

namespace Pathfinding {
	using Pathfinding.Drawing;

	public class NodeLink3Node : PointNode {
		public NodeLink3 link;
		public Vector3 portalA;
		public Vector3 portalB;

		public NodeLink3Node (AstarPath astar) {
        }

        public override bool GetPortal(GraphNode other, out Vector3 left, out Vector3 right)
        {
            left = default(Vector3);
            right = default(Vector3);
            return default;
        }

        public GraphNode GetOther(GraphNode a)
        {
            return default;
        }

        GraphNode GetOtherInternal(GraphNode a)
        {
            return default;
        }
    }

	/// <summary>
	/// Connects two TriangleMeshNodes (recast/navmesh graphs) as if they had shared an edge.
	/// Note: Usually you do not want to use this type of link, you want to use NodeLink2 or NodeLink (sorry for the not so descriptive names).
	/// </summary>
	[AddComponentMenu("Pathfinding/Link3")]
	[HelpURL("https://arongranberg.com/astar/documentation/stable/nodelink3.html")]
	public class NodeLink3 : GraphModifier {
		protected static Dictionary<GraphNode, NodeLink3> reference = new Dictionary<GraphNode, NodeLink3>();
		public static NodeLink3 GetNodeLink (GraphNode node) {
            return default;
        }

        /// <summary>End position of the link</summary>
        public Transform end;

		/// <summary>
		/// The connection will be this times harder/slower to traverse.
		/// Note that values lower than one will not always make the pathfinder choose this path instead of another path even though this one should
		/// lead to a lower total cost unless you also adjust the Heuristic Scale in A* Inspector -> Settings -> Pathfinding or disable the heuristic altogether.
		/// </summary>
		public float costFactor = 1.0f;

		public Transform StartTransform {
			get { return transform; }
		}

		public Transform EndTransform {
			get { return end; }
		}

		NodeLink3Node startNode;
		NodeLink3Node endNode;
		MeshNode connectedNode1, connectedNode2;
		Vector3 clamped1, clamped2;
		bool postScanCalled = false;

		public GraphNode StartNode {
			get { return startNode; }
		}

		public GraphNode EndNode {
			get { return endNode; }
		}

		public override void OnPostScan () {
        }

        public void InternalOnPostScan () {
        }

        public override void OnGraphsPostUpdateBeforeAreaRecalculation()
        {
        }

        protected override void OnEnable()
        {
        }

        protected override void OnDisable()
        {
        }

        void RemoveConnections(GraphNode node)
        {
        }

        [ContextMenu("Recalculate neighbours")]
        void ContextApplyForce()
        {
        }

        public void Apply(bool forceNewCheck)
        {
        }

        private readonly static Color GizmosColor = new Color(206.0f / 255.0f, 136.0f / 255.0f, 48.0f / 255.0f, 0.5f);
        private readonly static Color GizmosColorSelected = new Color(235.0f / 255.0f, 123.0f / 255.0f, 32.0f / 255.0f, 1.0f);

		public override void DrawGizmos () {
        }
    }
}
