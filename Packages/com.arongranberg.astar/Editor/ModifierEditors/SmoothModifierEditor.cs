using UnityEngine;
using UnityEditor;

namespace Pathfinding {
	[CustomEditor(typeof(SimpleSmoothModifier))]
	[CanEditMultipleObjects]
	public class SmoothModifierEditor : EditorBase {
		protected override void Inspector () {
        }
    }
}
