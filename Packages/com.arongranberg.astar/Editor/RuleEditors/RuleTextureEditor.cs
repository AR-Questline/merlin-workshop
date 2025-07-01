using Pathfinding.Graphs.Grid.Rules;
using UnityEditor;
using UnityEngine;

namespace Pathfinding {
	/// <summary>Editor for the <see cref="RuleTexture"/> rule</summary>
	[CustomGridGraphRuleEditor(typeof(RuleTexture), "Texture")]
	public class RuleTextureEditor : IGridGraphRuleEditor {
		protected static readonly string[] ChannelUseNames = { "None", "Penalty", "Height", "Walkability and Penalty", "Walkability" };

		public void OnInspectorGUI (GridGraph graph, GridGraphRule rule) {
        }

        static void SaveReferenceTexture(GridGraph graph)
        {
        }

        public void OnSceneGUI(GridGraph graph, GridGraphRule rule)
        {
        }
    }
}
