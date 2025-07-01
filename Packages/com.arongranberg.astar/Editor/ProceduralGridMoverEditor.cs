using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

namespace Pathfinding {
	[CustomEditor(typeof(ProceduralGraphMover))]
	[CanEditMultipleObjects]
	public class ProceduralGridMoverEditor : EditorBase {
		GUIContent[] graphLabels = new GUIContent[32];

		protected override void Inspector () {
        }
    }
}
