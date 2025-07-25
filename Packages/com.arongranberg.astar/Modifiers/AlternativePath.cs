using UnityEngine;
using System.Collections.Generic;

namespace Pathfinding {
	[AddComponentMenu("Pathfinding/Modifiers/Alternative Path Modifier")]
	[System.Serializable]
	/// <summary>
	/// Applies penalty to the paths it processes telling other units to avoid choosing the same path.
	///
	/// Note that this might not work properly if penalties are modified by other actions as well (e.g graph update objects which reset the penalty to zero).
	/// It will only work when all penalty modifications are relative, i.e adding or subtracting penalties, but not when setting penalties
	/// to specific values.
	///
	/// When destroyed, it will correctly remove any added penalty.
	/// </summary>
	[HelpURL("https://arongranberg.com/astar/documentation/stable/alternativepath.html")]
	public class AlternativePath : MonoModifier {
#if UNITY_EDITOR
		[UnityEditor.MenuItem("CONTEXT/Seeker/Add Alternative Path Modifier")]
		public static void AddComp (UnityEditor.MenuCommand command) {
        }
#endif

        public override int Order { get { return 10; } }

		/// <summary>How much penalty (weight) to apply to nodes</summary>
		public int penalty = 1000;

		/// <summary>Max number of nodes to skip in a row</summary>
		public int randomStep = 10;

		/// <summary>The previous path</summary>
		List<GraphNode> prevNodes = new List<GraphNode>();

		/// <summary>The previous penalty used. Stored just in case it changes during operation</summary>
		int prevPenalty;

		/// <summary>A random object</summary>
		readonly System.Random rnd = new System.Random();

		bool destroyed;

		public override void Apply (Path p) {
        }

        protected void OnDestroy () {
        }

        void ClearOnDestroy () {
        }

        void InversePrevious () {
        }

        void ApplyNow (List<GraphNode> nodes) {
        }
    }
}
