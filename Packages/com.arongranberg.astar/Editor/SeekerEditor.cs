using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.Linq;

namespace Pathfinding {
	[CustomEditor(typeof(Seeker))]
	[CanEditMultipleObjects]
	public class SeekerEditor : EditorBase {
		static bool tagPenaltiesOpen;
		static List<Seeker> scripts = new List<Seeker>();

		GUIContent[] exactnessLabels = new [] { new GUIContent("Node Center (Snap To Node)"), new GUIContent("Original"), new GUIContent("Interpolate (deprecated)"), new GUIContent("Closest On Node Surface"), new GUIContent("Node Connection") };

		protected override void Inspector () {
        }

        public static void TagsEditor(SerializedProperty tagPenaltiesProp, int[] traversableTags)
        {
        }
    }
}
