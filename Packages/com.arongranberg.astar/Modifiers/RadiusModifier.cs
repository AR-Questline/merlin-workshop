using UnityEngine;
using System;
using System.Collections.Generic;

namespace Pathfinding {
	/// <summary>
	/// Radius path modifier for offsetting paths.
	///
	/// The radius modifier will offset the path to create the effect
	/// of adjusting it to the characters radius.
	/// It gives good results on navmeshes which have not been offset with the
	/// character radius during scan. Especially useful when characters with different
	/// radiuses are used on the same navmesh. It is also useful when using
	/// rvo local avoidance with the RVONavmesh since the RVONavmesh assumes the
	/// navmesh has not been offset with the character radius.
	///
	/// This modifier assumes all paths are in the XZ plane (i.e Y axis is up).
	///
	/// It is recommended to use the Funnel Modifier on the path as well.
	///
	/// [Open online documentation to see images]
	///
	/// See: RVONavmesh
	/// See: modifiers
	///
	/// Also check out the howto page "Using Modifiers".
	///
	/// Since: Added in 3.2.6
	/// </summary>
	[AddComponentMenu("Pathfinding/Modifiers/Radius Offset Modifier")]
	[HelpURL("https://arongranberg.com/astar/documentation/stable/radiusmodifier.html")]
	public class RadiusModifier : MonoModifier {
#if UNITY_EDITOR
		[UnityEditor.MenuItem("CONTEXT/Seeker/Add Radius Modifier")]
		public static void AddComp (UnityEditor.MenuCommand command)
        {
        }
#endif

        public override int Order { get { return 41; } }

        /// <summary>
        /// Radius of the circle segments generated.
        /// Usually similar to the character radius.
        /// </summary>
        public float radius = 1f;

        /// <summary>
        /// Detail of generated circle segments.
        /// Measured as steps per full circle.
        ///
        /// It is more performant to use a low value.
        /// For movement, using a high value will barely improve path quality.
        /// </summary>
        public float detail = 10;

        /// <summary>
        /// Calculates inner tangents for a pair of circles.
        ///
        /// Add a to sigma to get the first tangent angle, subtract a from sigma to get the second tangent angle.
        ///
        /// Returns: True on success. False when the circles are overlapping.
        /// </summary>
        /// <param name="p1">Position of first circle</param>
        /// <param name="p2">Position of the second circle</param>
        /// <param name="r1">Radius of the first circle</param>
        /// <param name="r2">Radius of the second circle</param>
        /// <param name="a">Angle from the line joining the centers of the circles to the inner tangents.</param>
        /// <param name="sigma">World angle from p1 to p2 (in XZ space)</param>
        bool CalculateCircleInner(Vector3 p1, Vector3 p2, float r1, float r2, out float a, out float sigma)
        {
            a = default(float);
            sigma = default(float);
            return default;
        }

        /// <summary>
        /// Calculates outer tangents for a pair of circles.
        ///
        /// Add a to sigma to get the first tangent angle, subtract a from sigma to get the second tangent angle.
        ///
        /// Returns: True on success. False on failure (more specifically when |r1-r2| > |p1-p2| )
        /// </summary>
        /// <param name="p1">Position of first circle</param>
        /// <param name="p2">Position of the second circle</param>
        /// <param name="r1">Radius of the first circle</param>
        /// <param name="r2">Radius of the second circle</param>
        /// <param name="a">Angle from the line joining the centers of the circles to the inner tangents.</param>
        /// <param name="sigma">World angle from p1 to p2 (in XZ space)</param>
        bool CalculateCircleOuter(Vector3 p1, Vector3 p2, float r1, float r2, out float a, out float sigma)
        {
            a = default(float);
            sigma = default(float);
            return default;
        }

        [System.Flags]
        enum TangentType
        {
            OuterRight = 1 << 0,
			InnerRightLeft = 1 << 1,
			InnerLeftRight = 1 << 2,
			OuterLeft = 1 << 3,
			Outer = OuterRight | OuterLeft,
			Inner = InnerRightLeft | InnerLeftRight
		}

		TangentType CalculateTangentType (Vector3 p1, Vector3 p2, Vector3 p3, Vector3 p4) {
            return default;
        }

        TangentType CalculateTangentTypeSimple(Vector3 p1, Vector3 p2, Vector3 p3)
        {
            return default;
        }

        public override void Apply(Path p)
        {
        }

        float[] radi = new float[10];
		float[] a1 = new float[10];
		float[] a2 = new float[10];
		bool[] dir = new bool[10];

		/// <summary>Apply this modifier on a raw Vector3 list</summary>
		public List<Vector3> Apply (List<Vector3> vs) {
            return default;
        }
    }
}
