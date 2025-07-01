using UnityEngine;
using UnityEditor;

namespace Pathfinding {
	[CustomGraphEditor(typeof(PointGraph), "Point Graph")]
	public class PointGraphEditor : GraphEditor {
		static readonly GUIContent[] nearestNodeDistanceModeLabels = {
			new GUIContent("Node"),
			new GUIContent("Connection (slower)"),
		};

		public override void OnInspectorGUI (NavGraph target) {
        }
    }
}
