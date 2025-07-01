using UnityEngine;
using System.Collections.Generic;
using Pathfinding.Pooling;

namespace Pathfinding {
	[AddComponentMenu("Pathfinding/Modifiers/Simple Smooth Modifier")]
	[System.Serializable]
	[RequireComponent(typeof(Seeker))]
	/// <summary>
	/// Modifier which smooths the path. This modifier can smooth a path by either moving the points closer together (Simple) or using Bezier curves (Bezier).
	///
	/// Attach this component to the same GameObject as a Seeker component.
	///
	/// This component will hook in to the Seeker's path post-processing system and will post process any paths it searches for.
	/// Take a look at the Modifier Priorities settings on the Seeker component to determine where in the process this modifier should process the path.
	///
	/// Several smoothing types are available, here follows a list of them and a short description of what they do, and how they work.
	/// But the best way is really to experiment with them yourself.
	///
	/// - <b>Simple</b> Smooths the path by drawing all points close to each other. This results in paths that might cut corners if you are not careful.
	/// It will also subdivide the path to create more more points to smooth as otherwise it would still be quite rough.
	/// [Open online documentation to see images]
	/// - <b>Bezier</b> Smooths the path using Bezier curves. This results a smooth path which will always pass through all points in the path, but make sure it doesn't turn too quickly.
	/// [Open online documentation to see images]
	/// - <b>OffsetSimple</b> An alternative to Simple smooth which will offset the path outwards in each step to minimize the corner-cutting.
	/// But be careful, if too high values are used, it will turn into loops and look really ugly.
	/// - <b>Curved Non Uniform</b> [Open online documentation to see images]
	///
	/// Note: Modifies vectorPath array
	/// TODO: Make the smooth modifier take the world geometry into account when smoothing
	/// </summary>
	[HelpURL("https://arongranberg.com/astar/documentation/stable/simplesmoothmodifier.html")]
	public class SimpleSmoothModifier : MonoModifier {
#if UNITY_EDITOR
		[UnityEditor.MenuItem("CONTEXT/Seeker/Add Simple Smooth Modifier")]
		public static void AddComp (UnityEditor.MenuCommand command) {
        }
#endif

        public override int Order { get { return 50; } }

		/// <summary>Type of smoothing to use</summary>
		public SmoothType smoothType = SmoothType.Simple;

		/// <summary>Number of times to subdivide when not using a uniform length</summary>
		[Tooltip("The number of times to subdivide (divide in half) the path segments. [0...inf] (recommended [1...10])")]
		public int subdivisions = 2;

		/// <summary>Number of times to apply smoothing</summary>
		[Tooltip("Number of times to apply smoothing")]
		public int iterations = 2;

		/// <summary>Determines how much smoothing to apply in each smooth iteration. 0.5 usually produces the nicest looking curves.</summary>
		[Tooltip("Determines how much smoothing to apply in each smooth iteration. 0.5 usually produces the nicest looking curves")]
		[Range(0, 1)]
		public float strength = 0.5F;

		/// <summary>
		/// Toggle to divide all lines in equal length segments.
		/// See: <see cref="maxSegmentLength"/>
		/// </summary>
		[Tooltip("Toggle to divide all lines in equal length segments")]
		public bool uniformLength = true;

		/// <summary>
		/// The length of the segments in the smoothed path when using <see cref="uniformLength"/>.
		/// A high value yields rough paths and low value yields very smooth paths, but is slower
		/// </summary>
		[Tooltip("The length of each segment in the smoothed path. A high value yields rough paths and low value yields very smooth paths, but is slower")]
		public float maxSegmentLength = 2F;

		/// <summary>Length factor of the bezier curves' tangents'</summary>
		[Tooltip("Length factor of the bezier curves' tangents")]
		public float bezierTangentLength = 0.4F;

		/// <summary>Offset to apply in each smoothing iteration when using Offset Simple. See: <see cref="smoothType"/></summary>
		[Tooltip("Offset to apply in each smoothing iteration when using Offset Simple")]
		public float offset = 0.2F;

		/// <summary>Roundness factor used for CurvedNonuniform</summary>
		[Tooltip("How much to smooth the path. A higher value will give a smoother path, but might take the character far off the optimal path.")]
		public float factor = 0.1F;

		public enum SmoothType {
			Simple,
			Bezier,
			OffsetSimple,
			CurvedNonuniform
		}

		public override void Apply (Path p) {
        }

        public List<Vector3> CurvedNonuniform(List<Vector3> path)
        {
            return default;
        }

        public static Vector3 GetPointOnCubic(Vector3 a, Vector3 b, Vector3 tan1, Vector3 tan2, float t)
        {
            return default;
        }

        public List<Vector3> SmoothOffsetSimple(List<Vector3> path)
        {
            return default;
        }

        public List<Vector3> SmoothSimple(List<Vector3> path)
        {
            return default;
        }

        public List<Vector3> SmoothBezier (List<Vector3> path) {
            return default;
        }
    }
}
