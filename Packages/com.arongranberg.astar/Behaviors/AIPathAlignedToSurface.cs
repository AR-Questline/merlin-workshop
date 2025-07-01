using UnityEngine;
using System.Collections.Generic;
using UnityEngine.Profiling;

namespace Pathfinding {
	using Pathfinding.Pooling;
	using Unity.Collections.LowLevel.Unsafe;

	/// <summary>
	/// Movement script for curved worlds.
	/// This script inherits from AIPath, but adjusts its movement plane every frame using the ground normal.
	/// </summary>
	[HelpURL("https://arongranberg.com/astar/documentation/stable/aipathalignedtosurface.html")]
	public class AIPathAlignedToSurface : AIPath {
		/// <summary>Scratch dictionary used to avoid allocations every frame</summary>
		static readonly Dictionary<Mesh, int> scratchDictionary = new Dictionary<Mesh, int>();

		protected override void OnEnable () {
        }

        protected override void ApplyGravity (float deltaTime) {
        }

        /// <summary>
        /// Calculates smoothly interpolated normals for all raycast hits and uses that to set the movement planes of the agents.
        ///
        /// To support meshes that change at any time, we use Mesh.AcquireReadOnlyMeshData to get a read-only view of the mesh data.
        /// This is only efficient if we batch all updates and make a single call to Mesh.AcquireReadOnlyMeshData.
        ///
        /// This method is quite convoluted due to having to read the raw vertex data streams from unity meshes to avoid allocations.
        /// </summary>
        public static void UpdateMovementPlanes (AIPathAlignedToSurface[] components, int count) {
        }

        void SetInterpolatedNormal(Vector3 normal)
        {
        }

        protected override void UpdateMovementPlane()
        {
        }
    }
}
