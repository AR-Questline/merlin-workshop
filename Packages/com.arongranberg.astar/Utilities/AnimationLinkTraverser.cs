using System.Collections;
using UnityEngine;

namespace Pathfinding.Examples {
	using Pathfinding;

	/// <summary>
	/// Example of how to handle off-mesh link traversal.
	/// This is used in the "Example4_Recast_Navmesh2" example scene.
	///
	/// See: <see cref="Pathfinding.RichAI"/>
	/// See: <see cref="Pathfinding.RichAI.onTraverseOffMeshLink"/>
	/// See: <see cref="Pathfinding.AnimationLink"/>
	/// </summary>
	[HelpURL("https://arongranberg.com/astar/documentation/stable/animationlinktraverser.html")]
	public class AnimationLinkTraverser : VersionedMonoBehaviour {
		public Animation anim;

		RichAI ai;

		void OnEnable () {
        }

        void OnDisable () {
        }

        protected virtual IEnumerator TraverseOffMeshLink (RichSpecial rs) {
            return default;
        }
    }
}
