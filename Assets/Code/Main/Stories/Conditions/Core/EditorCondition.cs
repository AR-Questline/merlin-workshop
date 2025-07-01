using Awaken.TG.Main.Stories.Core;
using Awaken.TG.Main.Stories.Runtime;
using Awaken.TG.Main.Stories.Runtime.Nodes;

namespace Awaken.TG.Main.Stories.Conditions.Core {
    public abstract class EditorCondition : NodeElement {
        public StoryCondition CreateRuntimeCondition(StoryGraphParser parser) {
            var condition = CreateRuntimeConditionImpl(parser);
            if (condition != null) {
#if UNITY_EDITOR
                condition.DebugInfo = DebugInfo;
#endif
            }
            return condition;
        }
        protected abstract StoryCondition CreateRuntimeConditionImpl(StoryGraphParser parser);
    }
}