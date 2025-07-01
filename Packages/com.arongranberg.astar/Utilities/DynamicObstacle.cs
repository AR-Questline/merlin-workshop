using UnityEngine;
using System.Collections.Generic;

namespace Pathfinding {
	/// <summary>
	/// Updates graphs around the object as it moves.
	///
	/// Attach this script to any obstacle with a collider to enable dynamic updates of the graphs around it.
	/// When the object has moved or rotated at least <see cref="updateError"/> world units
	/// then it will call <see cref="AstarPath.UpdateGraphs"/> to update the graph around it.
	///
	/// Make sure that any children colliders do not extend beyond the bounds of the collider attached to the
	/// GameObject that the DynamicObstacle component is attached to, since this script only updates the graph
	/// around the bounds of the collider on the same GameObject.
	///
	/// An update will be triggered whenever the bounding box of the attached collider has changed (moved/expanded/etc.) by at least <see cref="updateError"/> world units or if
	/// the GameObject has rotated enough so that the outmost point of the object has moved at least <see cref="updateError"/> world units.
	///
	/// This script works with both 2D colliders and normal 3D colliders.
	///
	/// Note: This script works with a GridGraph, PointGraph, LayerGridGraph or RecastGraph.
	/// However, for recast graphs, you can often use the <see cref="NavmeshCut"/> instead, for simple obstacles. The <see cref="NavmeshCut"/> can be faster, but it's not quite as flexible.
	///
	/// See: AstarPath.UpdateGraphs
	/// See: graph-updates (view in online documentation for working links)
	/// See: navmeshcutting (view in online documentation for working links)
	/// </summary>
	[AddComponentMenu("Pathfinding/Dynamic Obstacle")]
#pragma warning disable 618 // Ignore obsolete warning
	[HelpURL("https://arongranberg.com/astar/documentation/stable/dynamicobstacle.html")]
	public class DynamicObstacle : GraphModifier, DynamicGridObstacle {
#pragma warning restore 618
		/// <summary>Collider to get bounds information from</summary>
		Collider coll;

		/// <summary>Cached transform component</summary>
		Transform tr;

		/// <summary>The minimum change in world units along one of the axis of the bounding box of the collider to trigger a graph update</summary>
		public float updateError = 1;

		/// <summary>
		/// Time in seconds between bounding box checks.
		/// If AstarPath.batchGraphUpdates is enabled, it is not beneficial to have a checkTime much lower
		/// than AstarPath.graphUpdateBatchingInterval because that will just add extra unnecessary graph updates.
		///
		/// In real time seconds (based on Time.realtimeSinceStartup).
		/// </summary>
		public float checkTime = 0.2F;

		/// <summary>Bounds of the collider the last time the graphs were updated</summary>
		Bounds prevBounds;

		/// <summary>Rotation of the collider the last time the graphs were updated</summary>
		Quaternion prevRotation;

		/// <summary>True if the collider was enabled last time the graphs were updated</summary>
		bool prevEnabled;

		float lastCheckTime = -9999;
		Queue<GraphUpdateObject> pendingGraphUpdates = new Queue<GraphUpdateObject>();

		Bounds bounds {
			get {
				if (coll != null) {
					return coll.bounds;
				}
				return default;
			}
		}

		bool colliderEnabled {
			get {
				return coll != null && coll.enabled;
			}
		}

		protected override void Awake () {
        }

        public override void OnPostScan () {
        }

        void Update () {
        }

        /// <summary>
        /// Revert graphs when disabled.
        /// When the DynamicObstacle is disabled or destroyed, a last graph update should be done to revert nodes to their original state
        /// </summary>
        protected override void OnDisable()
        {
        }

        /// <summary>
        /// Update the graphs around this object.
        /// Note: The graphs will not be updated immediately since the pathfinding threads need to be paused first.
        /// If you want to guarantee that the graphs have been updated then call <see cref="AstarPath.FlushGraphUpdates"/>
        /// after the call to this method.
        /// </summary>
        public void DoUpdateGraphs()
        {
        }

        /// <summary>Volume of a Bounds object. X*Y*Z</summary>
        static float BoundsVolume(Bounds b)
        {
            return default;
        }

        // bool RecastMeshObj.enabled { get => enabled; set => enabled = value; }
        float DynamicGridObstacle.updateError { get => updateError; set => updateError = value; }
		float DynamicGridObstacle.checkTime { get => checkTime; set => checkTime = value; }
	}

	/// <summary>
	/// Updates graphs around the object as it moves.
	/// Deprecated: Has been renamed to <see cref="DynamicObstacle"/>.
	/// </summary>
	[System.Obsolete("Has been renamed to DynamicObstacle")]
	public interface DynamicGridObstacle {
		bool enabled { get; set; }
		float updateError { get; set; }
		float checkTime { get; set; }
		void DoUpdateGraphs();
	}
}
