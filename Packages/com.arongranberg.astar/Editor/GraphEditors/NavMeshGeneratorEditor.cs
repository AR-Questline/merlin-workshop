using UnityEngine;
using UnityEditor;

namespace Pathfinding {
	[CustomGraphEditor(typeof(NavMeshGraph), "Navmesh Graph")]
	public class NavMeshGraphEditor : GraphEditor {
		public override void OnInspectorGUI (NavGraph target) {
        }
    }
}
