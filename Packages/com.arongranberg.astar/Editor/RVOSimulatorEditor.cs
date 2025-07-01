using UnityEditor;
using UnityEngine;

namespace Pathfinding {
	[CustomEditor(typeof(Pathfinding.RVO.RVOSimulator))]
	public class RVOSimulatorEditor : EditorBase {
		static readonly GUIContent[] movementPlaneOptions = new [] { new GUIContent("XZ (for 3D games)"), new GUIContent("XY (for 2D games)"), new GUIContent("Arbitrary (for non-planar worlds)") };

		protected override void Inspector () {
        }
    }
}
