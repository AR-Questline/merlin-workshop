using UnityEngine;
using System.Collections.Generic;
using Unity.Collections;

namespace Pathfinding.Graphs.Grid {
	using Pathfinding.Util;
	using Pathfinding.Graphs.Grid.Jobs;
	using Pathfinding.Jobs;

	/// <summary>
	/// Handles collision checking for graphs.
	/// Mostly used by grid based graphs
	/// </summary>
	[System.Serializable]
	public class GraphCollision {
		/// <summary>
		/// Collision shape to use.
		/// See: <see cref="ColliderType"/>
		/// </summary>
		public ColliderType type = ColliderType.Capsule;

		/// <summary>
		/// Diameter of capsule or sphere when checking for collision.
		/// When checking for collisions the system will check if any colliders
		/// overlap a specific shape at the node's position. The shape is determined
		/// by the <see cref="type"/> field.
		///
		/// A diameter of 1 means that the shape has a diameter equal to the node's width,
		/// or in other words it is equal to <see cref="Pathfinding.GridGraph.nodeSize"/>.
		///
		/// If <see cref="type"/> is set to Ray, this does not affect anything.
		///
		/// [Open online documentation to see images]
		/// </summary>
		public float diameter = 1F;

		/// <summary>
		/// Height of capsule or length of ray when checking for collision.
		/// If <see cref="type"/> is set to Sphere, this does not affect anything.
		///
		/// [Open online documentation to see images]
		///
		/// Warning: In contrast to Unity's capsule collider and character controller this height does not include the end spheres of the capsule, but only the cylinder part.
		/// This is mostly for historical reasons.
		/// </summary>
		public float height = 2F;

		/// <summary>
		/// Height above the ground that collision checks should be done.
		/// For example, if the ground was found at y=0, collisionOffset = 2
		/// type = Capsule and height = 3 then the physics system
		/// will be queried to see if there are any colliders in a capsule
		/// for which the bottom sphere that is made up of is centered at y=2
		/// and the top sphere has its center at y=2+3=5.
		///
		/// If type = Sphere then the sphere's center would be at y=2 in this case.
		/// </summary>
		public float collisionOffset;

		/// <summary>
		/// Direction of the ray when checking for collision.
		/// If <see cref="type"/> is not Ray, this does not affect anything
		///
		/// Deprecated: Only the Both mode is supported now.
		/// </summary>
		[System.Obsolete("Only the Both mode is supported now")]
		public RayDirection rayDirection = RayDirection.Both;

		/// <summary>Layers to be treated as obstacles.</summary>
		public LayerMask mask;

		/// <summary>Layers to be included in the height check.</summary>
		public LayerMask heightMask = -1;

		/// <summary>
		/// The height to check from when checking height ('ray length' in the inspector).
		///
		/// As the image below visualizes, different ray lengths can make the ray hit different things.
		/// The distance is measured up from the graph plane.
		///
		/// [Open online documentation to see images]
		/// </summary>
		public float fromHeight = 100;

		/// <summary>
		/// Toggles thick raycast.
		/// See: https://docs.unity3d.com/ScriptReference/Physics.SphereCast.html
		/// </summary>
		public bool thickRaycast;

		/// <summary>
		/// Diameter of the thick raycast in nodes.
		/// 1 equals <see cref="Pathfinding.GridGraph.nodeSize"/>
		/// </summary>
		public float thickRaycastDiameter = 1;

		/// <summary>Make nodes unwalkable when no ground was found with the height raycast. If height raycast is turned off, this doesn't affect anything.</summary>
		public bool unwalkableWhenNoGround = true;

		/// <summary>Toggle collision check</summary>
		public bool collisionCheck = true;

		/// <summary>
		/// Toggle height check. If false, the grid will be flat.
		///
		/// This setting will be ignored when 2D physics is used.
		/// </summary>
		public bool heightCheck = true;

		/// <summary>
		/// Direction to use as UP.
		/// See: Initialize
		/// </summary>
		public Vector3 up;

		/// <summary>
		/// <see cref="up"/> * <see cref="height"/>.
		/// See: Initialize
		/// </summary>
		private Vector3 upheight;

		/// <summary>
		/// <see cref="diameter"/> * scale * 0.5.
		/// Where scale usually is <see cref="Pathfinding.GridGraph.nodeSize"/>
		/// See: Initialize
		/// </summary>
		private float finalRadius;

		/// <summary>
		/// <see cref="thickRaycastDiameter"/> * scale * 0.5.
		/// Where scale usually is <see cref="Pathfinding.GridGraph.nodeSize"/> See: Initialize
		/// </summary>
		private float finalRaycastRadius;

		/// <summary>Offset to apply after each raycast to make sure we don't hit the same point again in CheckHeightAll</summary>
		public const float RaycastErrorMargin = 0.005F;

		/// <summary>
		/// Sets up several variables using the specified matrix and scale.
		/// See: GraphCollision.up
		/// See: GraphCollision.upheight
		/// See: GraphCollision.finalRadius
		/// See: GraphCollision.finalRaycastRadius
		/// </summary>
		public void Initialize (GraphTransform transform, float scale) {
        }

        /// <summary>
        /// Returns true if the position is not obstructed.
        /// If <see cref="collisionCheck"/> is false, this will always return true.
        /// </summary>
        public bool Check (Vector3 position) {
            return default;
        }

        /// <summary>
        /// Returns the position with the correct height.
        /// If <see cref="heightCheck"/> is false, this will return position.
        /// </summary>
        public Vector3 CheckHeight (Vector3 position) {
            return default;
        }

        /// <summary>
        /// Returns the position with the correct height.
        /// If <see cref="heightCheck"/> is false, this will return position.
        /// walkable will be set to false if nothing was hit.
        /// The ray will check a tiny bit further than to the grids base to avoid floating point errors when the ground is exactly at the base of the grid
        /// </summary>
        public Vector3 CheckHeight(Vector3 position, out RaycastHit hit, out bool walkable)
        {
            hit = default(RaycastHit);
            walkable = default(bool);
            return default;
        }

        /// <summary>Internal buffer used by <see cref="CheckHeightAll"/></summary>
        RaycastHit[] hitBuffer = new RaycastHit[8];

		/// <summary>
		/// Returns all hits when checking height for position.
		/// Warning: Does not work well with thick raycast, will only return an object a single time
		///
		/// Warning: The returned array is ephermal. It will be invalidated when this method is called again.
		/// If you need persistent results you should copy it.
		///
		/// The returned array may be larger than the actual number of hits, the numHits out parameter indicates how many hits there actually were.
		/// </summary>
		public RaycastHit[] CheckHeightAll (Vector3 position, out int numHits) {
            numHits = default(int);
            return default;
        }

        /// <summary>
        /// Returns if the position is obstructed for all nodes using the Ray collision checking method.
        /// collisionCheckResult[i] = true if there were no obstructions, false otherwise
        /// </summary>
        public void JobCollisionRay(NativeArray<Vector3> nodePositions, NativeArray<bool> collisionCheckResult, Vector3 up, Allocator allocationMethod, JobDependencyTracker dependencyTracker)
        {
        }

#if UNITY_2022_2_OR_NEWER
        public void JobCollisionCapsule(NativeArray<Vector3> nodePositions, NativeArray<bool> collisionCheckResult, Vector3 up, Allocator allocationMethod, JobDependencyTracker dependencyTracker)
        {
        }

        public void JobCollisionSphere(NativeArray<Vector3> nodePositions, NativeArray<bool> collisionCheckResult, Vector3 up, Allocator allocationMethod, JobDependencyTracker dependencyTracker)
        {
        }
#endif
    }

	/// <summary>
	/// Determines collision check shape.
	/// See: <see cref="GraphCollision"/>
	/// </summary>
	public enum ColliderType {
		/// <summary>Uses a Sphere, Physics.CheckSphere. In 2D this is a circle instead.</summary>
		Sphere,
		/// <summary>Uses a Capsule, Physics.CheckCapsule. This will behave identically to the Sphere mode in 2D.</summary>
		Capsule,
		/// <summary>Uses a Ray, Physics.Linecast. In 2D this is a single point instead.</summary>
		Ray
	}

	/// <summary>Determines collision check ray direction</summary>
	public enum RayDirection {
		Up,     /// <summary>< Casts the ray from the bottom upwards</summary>
		Down,   /// <summary>< Casts the ray from the top downwards</summary>
		Both    /// <summary>< Casts two rays in both directions</summary>
	}
}
