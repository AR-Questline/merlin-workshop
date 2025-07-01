using UnityEngine;
using UnityEditor;

namespace Pathfinding.RVO {
	[CustomEditor(typeof(RVOController))]
	[CanEditMultipleObjects]
	public class RVOControllerEditor : EditorBase {
		protected override void Inspector () {
        }
    }
}
