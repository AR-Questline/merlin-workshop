using System.Collections.ObjectModel;

namespace Pathfinding {
	using Pathfinding.Util;
	using Pathfinding.Collections;
	using UnityEngine;
	using System.Collections.Generic;

	/// <summary>Base class for the <see cref="NavmeshCut"/> and <see cref="NavmeshAdd"/> components</summary>
	[ExecuteAlways]
	public abstract class NavmeshClipper : VersionedMonoBehaviour {
		/// <summary>
		/// Distance between positions to require an update of the navmesh.
		/// A smaller distance gives better accuracy, but requires more updates when moving the object over time,
		/// so it is often slower.
		/// </summary>
		[Tooltip("Distance between positions to require an update of the navmesh\nA smaller distance gives better accuracy, but requires more updates when moving the object over time, so it is often slower.")]
		[SerializeField] protected float updateDistance = 0.4f;
		/// <summary>
		/// How many degrees rotation that is required for an update to the navmesh.
		/// Should be between 0 and 180.
		/// </summary>
		[Tooltip("How many degrees rotation that is required for an update to the navmesh. Should be between 0 and 180.")]
		public float updateRotationDistance = 10;
		/// <summary>
		/// Includes rotation and scale in calculations.
		/// This is slower since a lot more matrix multiplications are needed but gives more flexibility.
		/// </summary>
		[UnityEngine.Serialization.FormerlySerializedAsAttribute("useRotation")]
		public bool useRotationAndScale;
		
		public float UpdateDistance {
			get => updateDistance;
			set => updateDistance = value;
		}
		public float UpdateDistanceSq {
			get => updateDistance * updateDistance;
		}
		
		/// <summary>Called every time a NavmeshCut/NavmeshAdd component is enabled.</summary>
		static System.Action<NavmeshClipper> OnEnableCallback;

		/// <summary>Called every time a NavmeshCut/NavmeshAdd component is disabled.</summary>
		static System.Action<NavmeshClipper> OnDisableCallback;

		static readonly List<NavmeshClipper> all = new List<NavmeshClipper>();
		int listIndex = -1;

		/// <summary>
		/// Which graphs that are affected by this component.
		///
		/// You can use this to make a graph ignore a particular navmesh cut altogether.
		///
		/// Note that navmesh cuts can only affect navmesh/recast graphs.
		///
		/// If you change this field during runtime you must disable the component and enable it again for the changes to be detected.
		///
		/// See: <see cref="NavmeshBase.enableNavmeshCutting"/>
		/// </summary>
		public GraphMask graphMask = GraphMask.everything;

		/// <summary>
		/// Ensures that the list of enabled clippers is up to date.
		///
		/// This is useful when loading the scene, and some components may be enabled, but Unity has not yet called their OnEnable method.
		///
		/// See: <see cref="allEnabled"/>
		/// </summary>
		internal static void RefreshEnabledList () {
        }

        public static void AddEnableCallback (System.Action<NavmeshClipper> onEnable,  System.Action<NavmeshClipper> onDisable) {
        }

        public static void RemoveEnableCallback (System.Action<NavmeshClipper> onEnable,  System.Action<NavmeshClipper> onDisable) {
        }

        /// <summary>
        /// All navmesh clipper components in the scene.
        /// Not ordered in any particular way.
        /// Warning: Do not modify this list
        /// </summary>
        public static List<NavmeshClipper> allEnabled { get { return all; } }

		protected virtual void OnEnable () {
        }

        protected virtual void OnDisable()
        {
        }

        public abstract Rect GetBounds(GraphTransform transform, float radiusMargin);
	}
}
