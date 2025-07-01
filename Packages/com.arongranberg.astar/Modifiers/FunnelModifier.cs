using UnityEngine;
using System.Collections.Generic;
using Pathfinding.Pooling;

namespace Pathfinding {
	[AddComponentMenu("Pathfinding/Modifiers/Funnel Modifier")]
	[System.Serializable]
	/// <summary>
	/// Simplifies paths on navmesh graphs using the funnel algorithm.
	/// The funnel algorithm is an algorithm which can, given a path corridor with nodes in the path where the nodes have an area, like triangles, it can find the shortest path inside it.
	/// This makes paths on navmeshes look much cleaner and smoother.
	/// [Open online documentation to see images]
	///
	/// The funnel modifier also works on grid graphs using a different algorithm, but which yields visually similar results.
	/// See: <see cref="GridStringPulling"/>
	///
	/// Note: The <see cref="Pathfinding.RichAI"/> movement script has its own internal funnel modifier.
	/// You do not need to attach this component if you are using the RichAI movement script.
	///
	/// See: http://digestingduck.blogspot.se/2010/03/simple-stupid-funnel-algorithm.html
	/// </summary>
	[HelpURL("https://arongranberg.com/astar/documentation/stable/funnelmodifier.html")]
	public class FunnelModifier : MonoModifier {
		/// <summary>
		/// Determines if funnel simplification is used.
		/// When using the low quality setting only the funnel algorithm is used
		/// but when the high quality setting an additional step is done to simplify the path even more.
		///
		/// On tiled recast/navmesh graphs, but sometimes on normal ones as well, it can be good to simplify
		/// the funnel as a post-processing step to make the paths straighter.
		///
		/// This has a moderate performance impact during frames when a path calculation is completed.
		/// This is why it is disabled by default. For any units that you want high
		/// quality movement for you should enable it.
		///
		/// [Open online documentation to see images]
		///
		/// See: <see cref="Funnel.Simplify"/>
		///
		/// Note: This is only used for recast/navmesh graphs. Not for grid graphs.
		/// </summary>
		public FunnelQuality quality = FunnelQuality.Medium;

		/// <summary>
		/// Insert a vertex every time the path crosses a portal instead of only at the corners of the path.
		/// The resulting path will have exactly one vertex per portal if this is enabled.
		/// This may introduce vertices with the same position in the output (esp. in corners where many portals meet).
		/// [Open online documentation to see images]
		///
		/// Note: This is only used for recast/navmesh graphs. Not for grid graphs.
		/// </summary>
		public bool splitAtEveryPortal;

		/// <summary>
		/// When using a grid graph, take penalties, tag penalties and <see cref="ITraversalProvider"/> penalties into account.
		/// Enabling this is quite slow. It can easily make the modifier take twice the amount of time to run.
		/// So unless you are using penalties/tags/ITraversalProvider penalties that you need to take into account when simplifying
		/// the path, you should leave this disabled.
		/// </summary>
		public bool accountForGridPenalties = false;

		public enum FunnelQuality {
			Medium,
			High,
		}

#if UNITY_EDITOR
		[UnityEditor.MenuItem("CONTEXT/Seeker/Add Funnel Modifier")]
		public static void AddComp (UnityEditor.MenuCommand command) {
        }
#endif

        public override int Order { get { return 10; } }

        public override void Apply(Path p)
        {
        }
    }
}
