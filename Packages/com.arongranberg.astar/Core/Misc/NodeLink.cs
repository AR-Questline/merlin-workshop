using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Pathfinding {
	using Pathfinding.Util;
	using Pathfinding.Drawing;

	/// <summary>
	/// Connects two nodes with a direct connection.
	/// It is not possible to detect this link when following a path (which may be good or bad), for that you can use NodeLink2.
	///
	/// See: editing-graphs (view in online documentation for working links)
	/// </summary>
	[AddComponentMenu("Pathfinding/Link")]
	[HelpURL("https://arongranberg.com/astar/documentation/stable/nodelink.html")]
	public class NodeLink : GraphModifier {
		/// <summary>End position of the link</summary>
		public Transform end;

		/// <summary>
		/// The connection will be this times harder/slower to traverse.
		/// Note that values lower than one will not always make the pathfinder choose this path instead of another path even though this one should
		/// lead to a lower total cost unless you also adjust the Heuristic Scale in A* Inspector -> Settings -> Pathfinding or disable the heuristic altogether.
		/// </summary>
		public float costFactor = 1.0f;

		/// <summary>Make a one-way connection</summary>
		public bool oneWay = false;

		/// <summary>Delete existing connection instead of adding one</summary>
		public bool deleteConnection = false;

		public Transform Start {
			get { return transform; }
		}

		public Transform End {
			get { return end; }
		}

		public override void OnGraphsPostUpdateBeforeAreaRecalculation () {
        }

        public static void DrawArch (Vector3 a, Vector3 b, Vector3 up, Color color) {
        }

        /// <summary>
        /// Connects the start and end points using a link or refreshes the existing link.
        ///
        /// If you have moved the link or otherwise modified it you need to call this method.
        ///
        /// Warning: This must only be done when it is safe to update the graph structure.
        /// The easiest is to do it inside a work item. See <see cref="AstarPath.AddWorkItem"/>.
        /// </summary>
        public virtual void Apply () {
        }

        public override void DrawGizmos()
        {
        }

#if UNITY_EDITOR
        [UnityEditor.MenuItem("Edit/Pathfinding/Link Pair %&l")]
        public static void LinkObjects()
        {
        }

        [UnityEditor.MenuItem("Edit/Pathfinding/Unlink Pair %&u")]
        public static void UnlinkObjects()
        {
        }

        [UnityEditor.MenuItem("Edit/Pathfinding/Delete Links on Selected %&b")]
		public static void DeleteLinks () {
        }

        public static void LinkObjects (Transform a, Transform b, bool removeConnection) {
        }
#endif
    }
}
