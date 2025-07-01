using UnityEngine;
using UnityEditor;

namespace Pathfinding {
	[CustomEditor(typeof(NavmeshCut))]
	[CanEditMultipleObjects]
	public class NavmeshCutEditor : EditorBase {
		GUIContent[] MeshTypeOptions = new [] {
			new GUIContent("Rectangle (legacy)"),
			new GUIContent("Circle (legacy)"),
			new GUIContent("Custom Mesh"),
			new GUIContent("Box"),
			new GUIContent("Sphere"),
			new GUIContent("Capsule"),
		};

		protected override void Inspector () {
        }
    }
}
