using Awaken.TG.Main.Stories.Core;
using Awaken.TG.Main.Stories.Runtime;
using Awaken.TG.Main.Stories.Runtime.Nodes;

namespace Awaken.TG.Main.Stories.Conditions.Core {
    /// <summary>
    /// Represents conditions attached to a step. If the conditions aren't fulfilled,
    /// the step is not executed.
    /// </summary>
    [NodeWidth(280)]
    [NodeTint(0.6f, 0.3f, 0.3f)]
    [CreateNodeMenu("0021/AND Condition", typeof(StoryGraph))]
    public class AndEditorNode : ConditionsEditorNode {
        public override StoryConditions CreateRuntimeConditions(StoryGraphParser parser) {
            return new StoryAndConditions();
        }
    }
}
