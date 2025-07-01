using Pathfinding.Graphs.Grid.Rules;
using UnityEditor;
using UnityEngine;

namespace Pathfinding {
	/// <summary>Editor for the <see cref="RuleElevationPenalty"/> rule</summary>
	[CustomGridGraphRuleEditor(typeof(RuleElevationPenalty), "Penalty from Elevation")]
	public class RuleElevationPenaltyEditor : IGridGraphRuleEditor {
		float lastChangedTime = -10000;

		public void OnInspectorGUI (GridGraph graph, GridGraphRule rule) {
        }

        protected static readonly Color GizmoColorMax = new Color(222.0f / 255, 113.0f / 255, 33.0f / 255, 0.5f);
        protected static readonly Color GizmoColorMin = new Color(33.0f / 255, 104.0f / 255, 222.0f / 255, 0.5f);

        public void OnSceneGUI(GridGraph graph, GridGraphRule rule)
        {
        }
    }
}
