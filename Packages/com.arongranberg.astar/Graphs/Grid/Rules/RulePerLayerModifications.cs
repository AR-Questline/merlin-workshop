using Pathfinding.Jobs;

namespace Pathfinding.Graphs.Grid.Rules {
	/// <summary>
	/// Modifies nodes based on the layer of the surface under the node.
	///
	/// You can for example make all surfaces with a specific layer make the nodes get a specific tag.
	///
	/// This uses the information from the height testing system to determine which layer the surface is on.
	/// As a consequence, this rule does not do anything when using 2D physics, or height testing is disabled on the grid graph.
	///
	/// [Open online documentation to see images]
	///
	/// See: grid-rules (view in online documentation for working links)
	/// </summary>
	[Pathfinding.Util.Preserve]
	public class RulePerLayerModifications : GridGraphRule {
		public PerLayerRule[] layerRules = new PerLayerRule[0];
		const int SetTagBit = 1 << 30;

		public struct PerLayerRule {
			/// <summary>Layer this rule applies to</summary>
			public int layer;
			/// <summary>The action to apply to matching nodes</summary>
			public RuleAction action;
			/// <summary>
			/// Tag for the RuleAction.SetTag action.
			/// Must be between 0 and <see cref="Pathfinding.GraphNode.MaxTagIndex"/>
			/// </summary>
			public int tag;
		}

		public enum RuleAction {
			/// <summary>Sets the tag of all affected nodes to <see cref="PerLayerRule.tag"/></summary>
			SetTag,
			/// <summary>Makes all affected nodes unwalkable</summary>
			MakeUnwalkable,
		}

		public override void Register (GridGraphRules rules) {
        }
    }
}
