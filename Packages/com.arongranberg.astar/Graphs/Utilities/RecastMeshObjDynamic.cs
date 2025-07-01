using UnityEngine;
using Pathfinding.Util;

namespace Pathfinding {
	/// <summary>
	/// Dynamic and static are separate to remove enable disable overhead
	/// 
	/// Explicit mesh object for recast graphs.
	///
	/// Sometimes you want to tweak the navmesh on a per-object basis. For example you might want to make some objects completely unwalkable, or you might want to special case some objects to remove them from the navmesh altogether.
	///
	/// You can do this using the <see cref="RecastMeshObjBase"/> component. Attach it to any object you want to modify and configure the settings as you wish.
	///
	/// Using the <see cref="RecastMeshObjBase"/> component you can:
	///
	/// - Exclude an object from the graph completely.
	/// - Make the surfaces of an object unwalkable.
	/// - Make the surfaces of an object walkable (this is just the default behavior).
	/// - Create seams in the navmesh between adjacent objects.
	/// - Mark the surfaces of an object with a specific tag (see tags) (view in online documentation for working links).
	///
	/// Adding this component to an object will make sure it is included in any recast graphs.
	/// It will be included even if the Rasterize Meshes toggle is set to false.
	///
	/// Using RecastMeshObjs instead of relying on the Rasterize Meshes option is good for several reasons.
	/// - Rasterize Meshes is slow. If you are using a tiled graph and you are updating it, every time something is recalculated
	/// the graph will have to search all meshes in your scene for ones to rasterize. In contrast, RecastMeshObjs are stored
	/// in a tree for extremely fast lookup (O(log n + k) compared to O(n) where n is the number of meshes in your scene and k is the number of meshes
	/// which should be rasterized, if you know Big-O notation).
	/// - The RecastMeshObj exposes some options which can not be accessed using the Rasterize Meshes toggle. See member documentation for more info.
	///      This can for example be used to include meshes in the recast graph rasterization, but make sure that the character cannot walk on them.
	///
	/// Since the objects are stored in a tree, and trees are slow to update, there is an enforcement that objects are not allowed to move
	/// unless the <see cref="dynamic"/> option is enabled. When the dynamic option is enabled, the object will be stored in an array instead of in the tree.
	/// This will reduce the performance improvement over 'Rasterize Meshes' but is still faster.
	///
	/// If a mesh filter and a mesh renderer is attached to this GameObject, those will be used in the rasterization
	/// otherwise if a collider is attached, that will be used.
	/// </summary>
	[AddComponentMenu("Pathfinding/Navmesh/RecastMeshObj Dynamic")]
	[DisallowMultipleComponent]
	[HelpURL("https://arongranberg.com/astar/documentation/stable/class_pathfinding_1_1_recast_mesh_obj.php")]
	public class RecastMeshObjDynamic : RecastMeshObjStatic {
		public override bool dynamic => true;

		void OnEnable () {
        }

        public override void Enable() {
        }

        void OnDisable () {
        }

        public void Disable() {
        }

        static void OnUpdate(RecastMeshObjDynamic[] components, int _)
        {
        }
    }
}
