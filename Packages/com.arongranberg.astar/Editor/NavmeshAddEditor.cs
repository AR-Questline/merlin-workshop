using UnityEngine;
using UnityEditor;

namespace Pathfinding {
	[CustomEditor(typeof(NavmeshAdd))]
	[CanEditMultipleObjects]
	public class NavmeshAddEditor : EditorBase {
		protected override void Inspector () {
        }
    }
}
