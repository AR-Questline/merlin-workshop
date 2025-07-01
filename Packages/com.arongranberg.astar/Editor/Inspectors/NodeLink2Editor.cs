using Pathfinding;
using UnityEditor;
using UnityEngine;

namespace Pathfinding {
	[CustomEditor(typeof(NodeLink2), true)]
	[CanEditMultipleObjects]
	public class NodeLink2Editor : EditorBase {
		GUIContent HandlerContent = new GUIContent("Handler", "The object that handles movement when traversing the link");

		protected override void Inspector () {
        }
    }
}
