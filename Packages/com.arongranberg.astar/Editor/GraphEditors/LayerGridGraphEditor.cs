using UnityEngine;
using UnityEditor;
using Pathfinding.Graphs.Grid;

namespace Pathfinding {
	[CustomGraphEditor(typeof(LayerGridGraph), "Layered Grid Graph")]
	public class LayerGridGraphEditor : GridGraphEditor {
		protected override void DrawMiddleSection (GridGraph graph) {
        }

        protected override void DrawMaxClimb(GridGraph graph)
        {
        }

        protected override void DrawCollisionEditor (GraphCollision collision) {
        }
    }
}
