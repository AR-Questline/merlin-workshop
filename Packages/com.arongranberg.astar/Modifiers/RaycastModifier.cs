using UnityEngine;
using System.Collections.Generic;

namespace Pathfinding {
	using Pathfinding.Util;
	using Pathfinding.Pooling;

	/// <summary>
	/// Simplifies a path using raycasting.
	///
	/// This modifier will try to remove as many nodes as possible from the path using raycasting (linecasting) to validate the node removal.
	/// You can use either graph raycasting or Physics.Raycast.
	/// When using graph raycasting, the graph will be traversed and checked for obstacles. When physics raycasting is used, the Unity physics system
	/// will be asked if there are any colliders which intersect the line that is currently being checked.
	///
	/// See: https://docs.unity3d.com/ScriptReference/Physics.html
	/// See: <see cref="Pathfinding.IRaycastableGraph"/>
	///
	/// This modifier is primarily intended for grid graphs and layered grid graphs. Though depending on your game it may also be
	/// useful for point graphs. However note that point graphs do not have any built-in raycasting so you need to use physics raycasting for that graph.
	///
	/// For navmesh/recast graphs the <see cref="Pathfinding.FunnelModifier"/> is a much better and faster alternative.
	///
	/// On grid graphs you can combine the FunnelModifier with this modifier by simply attaching both of them to a GameObject with a Seeker.
	/// This may or may not give you better results. It will usually follow the border of the graph more closely when they are both used
	/// however it more often misses some simplification opportunities.
	/// When both modifiers are used then the funnel modifier will run first and simplify the path, and then this modifier will take
	/// the output from the funnel modifier and try to simplify that even more.
	///
	/// This modifier has several different quality levels. The highest quality is significantly slower than the
	/// lowest quality level (10 times slower is not unusual). So make sure you pick the lowest quality that your game can get away with.
	/// You can use the Unity profiler to see if it takes up any significant amount of time. It will show up under the heading "Running Path Modifiers".
	///
	/// [Open online documentation to see images]
	///
	/// See: modifiers (view in online documentation for working links)
	/// </summary>
	[AddComponentMenu("Pathfinding/Modifiers/Raycast Modifier")]
	[RequireComponent(typeof(Seeker))]
	[System.Serializable]
	[HelpURL("https://arongranberg.com/astar/documentation/stable/raycastmodifier.html")]
	public class RaycastModifier : MonoModifier {
#if UNITY_EDITOR
		[UnityEditor.MenuItem("CONTEXT/Seeker/Add Raycast Simplifier Modifier")]
		public static void AddComp (UnityEditor.MenuCommand command) {
        }
#endif

        public override int Order { get { return 40; } }

		/// <summary>Use Physics.Raycast to simplify the path</summary>
		public bool useRaycasting = false;

		/// <summary>
		/// Layer mask used for physics raycasting.
		/// All objects with layers that are included in this mask will be treated as obstacles.
		/// If you are using a grid graph you usually want this to be the same as the mask in the grid graph's 'Collision Testing' settings.
		/// </summary>
		public LayerMask mask = -1;

		/// <summary>
		/// Checks around the line between two points, not just the exact line.
		/// Make sure the ground is either too far below or is not inside the mask since otherwise the raycast might always hit the ground.
		///
		/// See: https://docs.unity3d.com/ScriptReference/Physics.SphereCast.html
		/// </summary>
		[Tooltip("Checks around the line between two points, not just the exact line.\nMake sure the ground is either too far below or is not inside the mask since otherwise the raycast might always hit the ground.")]
		public bool thickRaycast;

		/// <summary>Distance from the ray which will be checked for colliders</summary>
		[Tooltip("Distance from the ray which will be checked for colliders")]
		public float thickRaycastRadius;

		/// <summary>
		/// Check for intersections with 2D colliders instead of 3D colliders.
		/// Useful for 2D games.
		///
		/// See: https://docs.unity3d.com/ScriptReference/Physics2D.html
		/// </summary>
		[Tooltip("Check for intersections with 2D colliders instead of 3D colliders.")]
		public bool use2DPhysics;

		/// <summary>
		/// Offset from the original positions to perform the raycast.
		/// Can be useful to avoid the raycast intersecting the ground or similar things you do not want to it intersect
		/// </summary>
		[Tooltip("Offset from the original positions to perform the raycast.\nCan be useful to avoid the raycast intersecting the ground or similar things you do not want to it intersect")]
		public Vector3 raycastOffset = Vector3.zero;

		/// <summary>Use raycasting on the graphs. Only currently works with GridGraph and NavmeshGraph and RecastGraph. </summary>
		[Tooltip("Use raycasting on the graphs. Only currently works with GridGraph and NavmeshGraph and RecastGraph. This is a pro version feature.")]
		public bool useGraphRaycasting = true;


		/// <summary>
		/// Higher quality modes will try harder to find a shorter path.
		/// Higher qualities may be significantly slower than low quality.
		/// [Open online documentation to see images]
		/// </summary>
		[Tooltip("When using the high quality mode the script will try harder to find a shorter path. This is significantly slower than the greedy low quality approach.")]
		public Quality quality = Quality.Medium;

		public enum Quality {
			/// <summary>One iteration using a greedy algorithm</summary>
			Low,
			/// <summary>Two iterations using a greedy algorithm</summary>
			Medium,
			/// <summary>One iteration using a dynamic programming algorithm</summary>
			High,
			/// <summary>Three iterations using a dynamic programming algorithm</summary>
			Highest
		}

		static readonly int[] iterationsByQuality = new [] { 1, 2, 1, 3 };
		static List<Vector3> buffer = new List<Vector3>();
		static float[] DPCosts = new float[16];
		static int[] DPParents = new int[16];

		Filter cachedFilter = new Filter();

		NNConstraint cachedNNConstraint = NNConstraint.None;

		class Filter {
			public Path path;
			public readonly System.Func<GraphNode, bool> cachedDelegate;

			public Filter() {
            }

            bool CanTraverse (GraphNode node) {
                return default;
            }
        }

		public override void Apply (Path p) {
        }

        List<Vector3> ApplyGreedy(Path p, List<Vector3> points, System.Func<GraphNode, bool> filter, NNConstraint nnConstraint)
        {
            return default;
        }

        List<Vector3> ApplyDP(Path p, List<Vector3> points, System.Func<GraphNode, bool> filter, NNConstraint nnConstraint)
        {
            return default;
        }

        /// <summary>
        /// Check if a straight path between v1 and v2 is valid.
        /// If both n1 and n2 are supplied it is assumed that the line goes from the center of n1 to the center of n2 and a more optimized graph linecast may be done.
        /// </summary>
        protected bool ValidateLine(GraphNode n1, GraphNode n2, Vector3 v1, Vector3 v2, System.Func<GraphNode, bool> filter, NNConstraint nnConstraint)
        {
            return default;
        }
    }
}
