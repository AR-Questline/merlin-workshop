using UnityEditor;
using UnityEngine;
using System.Linq;
using Pathfinding.Graphs.Grid.Rules;

namespace Pathfinding {
	/// <summary>Editor for the <see cref="RulePerLayerModifications"/> rule</summary>
	[CustomGridGraphRuleEditor(typeof(RulePerLayerModifications), "Per Layer Modifications")]
	public class RulePerLayerModificationsEditor : IGridGraphRuleEditor {
		public void OnInspectorGUI (GridGraph graph, GridGraphRule rule) {
        }

        public void OnSceneGUI(GridGraph graph, GridGraphRule rule)
        {
        }
    }
}
