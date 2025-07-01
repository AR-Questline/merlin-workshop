using Awaken.TG.Main.Stories.Api;
using Awaken.TG.Main.Stories.Core.Attributes;
using Awaken.TG.Main.Stories.Execution;
using Awaken.TG.Main.Stories.Runtime;
using Awaken.TG.Main.Stories.Runtime.Nodes;
using Awaken.TG.Main.Stories.Steps.Helpers;

namespace Awaken.TG.Main.Stories.Steps {
    /// <summary>
    /// Sets a variable than can be checked elsewhere.
    /// </summary>
    [Element("Variables/Variable: Add")]
    public class SEditorVariableAdd : SEditorVariableReference {
        public override string extraInfo => "";

        protected override StoryStep CreateRuntimeStepImpl(StoryGraphParser parser) {
            return new SVariableAdd {
                context = context,
                contexts = contexts,
                var = var,
                comment = comment
            };
        }
    }
    
    public partial class SVariableAdd : SVariableReference {
        public override StepResult Execute(Story story) {
            var.Prepare(story, context, contexts).AddValue();
            if (!string.IsNullOrEmpty(comment)) {
                story.ShowText(TextConfig.WithTextAndStyle($"[{comment}]", StoryTextStyle.Aside));
            }
            return StepResult.Immediate;
        }
    }
}