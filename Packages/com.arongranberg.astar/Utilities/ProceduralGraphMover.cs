using UnityEngine;
using System.Collections;

namespace Pathfinding {
	using Pathfinding.Util;
	using Unity.Mathematics;
	using UnityEngine.Profiling;
	using Pathfinding.Graphs.Navmesh;
	using Pathfinding.Jobs;
	using Pathfinding.Drawing;
	using System.Collections.Generic;
	using Unity.Jobs;

	/// <summary>
	/// Moves a grid or recast graph to follow a target.
	///
	/// This is useful if you have a very large, or even infinite, world, but pathfinding is only necessary in a small region around an object (for example the player).
	/// This component will move a graph around so that its center stays close to the <see cref="target"/> object.
	///
	/// Note: This component can only be used with grid graphs, layered grid graphs and (tiled) recast graphs.
	///
	/// <b>Usage</b>
	/// Take a look at the example scene called "Procedural" for an example of how to use this script
	///
	/// Attach this to some object in the scene and assign the target to e.g the player.
	/// Then the graph will follow that object around as it moves.
	///
	/// [Open online documentation to see videos]
	///
	/// [Open online documentation to see videos]
	///
	/// <b>Performance</b>
	/// When the graph is moved you may notice an fps drop.
	/// If this grows too large you can try a few things:
	///
	/// General advice:
	/// - Turn on multithreading (A* Inspector -> Settings)
	/// - Make sure you have 'Show Graphs' disabled in the A* inspector, since gizmos in the scene view can take some
	///   time to update when the graph moves, and thus make it seem like this script is slower than it actually is.
	///
	/// For grid graphs:
	/// - Avoid using any erosion in the grid graph settings. This is relatively slow. Each erosion iteration requires expanding the region that is updated by 1 node.
	/// - Reduce the grid size or resolution.
	/// - Reduce the <see cref="updateDistance"/>. This will make the updates smaller but more frequent.
	///   This only works to some degree however since an update has an inherent overhead.
	/// - Disable Height Testing or Collision Testing in the grid graph if you can. This can give a performance boost
	///   since fewer calls to the physics engine need to be done.
	///
	/// For recast graphs:
	/// - Rasterize colliders instead of meshes. This is typically faster.
	/// - Use a reasonable tile size. Very small tiles can cause more overhead, and too large tiles might mean that you are updating too much in one go.
	///    Typical values are around 64 to 256 voxels.
	/// - Use a larger cell size. A lower cell size will give better quality graphs, but it will also be slower to scan.
	///
	/// The graph updates will be offloaded to worker threads as much as possible.
	///
	/// See: large-worlds (view in online documentation for working links)
	/// </summary>
	[AddComponentMenu("Pathfinding/Procedural Graph Mover")]
	[HelpURL("https://arongranberg.com/astar/documentation/stable/proceduralgraphmover.html")]
	public class ProceduralGraphMover : VersionedMonoBehaviour {
		/// <summary>
		/// Grid graphs will be updated if the target is more than this number of nodes from the graph center.
		/// Note that this is in nodes, not world units.
		///
		/// Note: For recast graphs, this setting has no effect.
		/// </summary>
		public float updateDistance = 10;

		/// <summary>Graph will be moved to follow this target</summary>
		public Transform target;

		/// <summary>True while the graph is being updated by this script</summary>
		public bool updatingGraph { get; private set; }

		/// <summary>
		/// Graph to update.
		/// This will be set at Start based on <see cref="graphIndex"/>.
		/// During runtime you may set this to any graph or to null to disable updates.
		/// </summary>
		public NavGraph graph;

		/// <summary>
		/// Index for the graph to update.
		/// This will be used at Start to set <see cref="graph"/>.
		///
		/// This is an index into the AstarPath.active.data.graphs array.
		/// </summary>
		[HideInInspector]
		public int graphIndex;

		void Start () {
        }

        void OnDisable () {
        }

        /// <summary>Update is called once per frame</summary>
        void Update ()
        {
        }

        /// <summary>
        /// Updates the graph asynchronously.
        /// This will move the graph so that the target's position is (roughly) the center of the graph.
        /// If the graph is already being updated, the call will be ignored.
        ///
        /// The image below shows which nodes will be updated when the graph moves.
        /// The whole graph is not recalculated each time it is moved, but only those
        /// nodes that have to be updated, the rest will keep their old values.
        /// The image is a bit simplified but it shows the main idea.
        /// [Open online documentation to see images]
        ///
        /// If you want to move the graph synchronously then pass false to the async parameter.
        /// </summary>
        public void UpdateGraph(bool async = true)
        {
        }

        void UpdateGridGraph(GridGraph graph, bool async)
        {
        }

        static Vector2Int RecastGraphTileShift(RecastGraph graph, Vector3 targetCenter)
        {
            return default;
        }

        void UpdateRecastGraph(RecastGraph graph, Vector2Int delta, bool async)
        {
        }
    }

    /// <summary>
    /// This class has been renamed to <see cref="ProceduralGraphMover"/>.
    ///
    /// Deprecated: Use <see cref="ProceduralGraphMover"/> instead
    /// </summary>
    [System.Obsolete("This class has been renamed to ProceduralGraphMover", true)]
    public class ProceduralGridMover
    {
    }
}
