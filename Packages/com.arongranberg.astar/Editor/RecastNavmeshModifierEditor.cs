using UnityEngine;
using UnityEditor;

namespace Pathfinding {
	[CustomEditor(typeof(RecastMeshObjStatic))]
	[CanEditMultipleObjects]
	public class RecastNavmeshModifierEditor : EditorBase {
		protected override void Inspector () {
        }
    }
}
