using Pathfinding.Graphs.Grid.Rules;
using UnityEditor;
using UnityEngine;

namespace Pathfinding {
	/// <summary>Editor for the <see cref="RuleAnglePenalty"/> rule</summary>
	[CustomGridGraphRuleEditor(typeof(RuleAnglePenalty), "Penalty from Slope Angle")]
	public class RuleAnglePenaltyEditor : IGridGraphRuleEditor {
		public void OnInspectorGUI (GridGraph graph, GridGraphRule rule) {
        }

        public void OnSceneGUI(GridGraph graph, GridGraphRule rule)
        {
        }
    }
}
