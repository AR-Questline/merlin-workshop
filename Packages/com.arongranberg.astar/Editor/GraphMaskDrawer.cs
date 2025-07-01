using UnityEditor;
using UnityEngine;

namespace Pathfinding {
	[CustomPropertyDrawer(typeof(GraphMask))]
	public class GraphMaskDrawer : PropertyDrawer {
		string[] graphLabels = new string[32];

		public override void OnGUI (Rect position, SerializedProperty property, GUIContent label) {
        }
    }
}
