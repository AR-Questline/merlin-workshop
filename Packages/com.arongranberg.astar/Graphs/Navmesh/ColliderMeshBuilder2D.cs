using UnityEngine;
using System.Collections.Generic;
using Unity.Mathematics;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Burst;
using UnityEngine.Assertions;
using UnityEngine.Profiling;
using Pathfinding.Util;
using Pathfinding.Collections;
using UnityEngine.Tilemaps;


namespace Pathfinding.Graphs.Navmesh {
	[BurstCompile]
	struct CircleGeometryUtilities {
		/// <summary>
		/// Cached values for CircleRadiusAdjustmentFactor.
		///
		/// We can calculate the area of a polygonized circle, and equate that with the area of a unit circle
		/// <code>
		/// x * cos(math.PI / steps) * sin(math.PI/steps) * steps = pi
		/// </code>
		/// Solving for the factor that makes them equal (x) gives the expression below.
		///
		/// Generated using the python code:
		/// <code>
		/// [math.sqrt(2 * math.pi / (i * math.sin(2 * math.pi / i))) for i in range(3, 23)]
		/// </code>
		///
		/// It would be nice to generate this using a static constructor, but that is not supported by Unity's burst compiler.
		/// </summary>
		static readonly float[] circleRadiusAdjustmentFactors = new float[] {
			1.56f, 1.25f, 1.15f, 1.1f, 1.07f, 1.05f, 1.04f, 1.03f, 1.03f, 1.02f, 1.02f, 1.02f, 1.01f, 1.01f, 1.01f, 1.01f, 1.01f, 1.01f, 1.01f, 1.01f,
		};

		/// <summary>The number of steps required to get a circle with a maximum error of maxError</summary>
		public static int CircleSteps (Matrix4x4 matrix, float radius, float maxError) {
            return default;
        }

        /// <summary>
        /// Radius factor to adjust for circle approximation errors.
        /// If a circle is approximated by fewer segments, it will be slightly smaller than the original circle.
        /// This factor is used to adjust the radius of the circle so that the resulting circle will have roughly the same area as the original circle.
        /// </summary>
#if MODULE_COLLECTIONS_2_0_0_OR_NEWER && UNITY_2022_2_OR_NEWER
        [GenerateTestsForBurstCompatibility]
#endif
        public static float CircleRadiusAdjustmentFactor(int steps)
        {
            return default;
        }
    }
}
