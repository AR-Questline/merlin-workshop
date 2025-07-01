using Awaken.TG.Main.Localization;
using Awaken.TG.Main.Stories.Api;
using Awaken.TG.Main.Stories.Core.Attributes;
using Awaken.TG.Main.Stories.Execution;
using Awaken.TG.Main.Stories.Runtime;
using Awaken.TG.Main.Stories.Runtime.Nodes;
using Awaken.TG.Main.Stories.Steps.Helpers;
using Awaken.TG.Utility;
using Awaken.Utility.Collections;

namespace Awaken.TG.Main.Stories.Steps {
    [Element("Variables/Variable: Multiply")]
    public class SEditorVariableMultiply : SEditorVariableReference {
        public override string extraInfo => "";

        protected override StoryStep CreateRuntimeStepImpl(StoryGraphParser parser) {
            return new SVariableMultiply {
                context = context,
                contexts = contexts,
                var = var,
                comment = comment,
            };
        }
    }
    
    public partial class SVariableMultiply : SVariableReference {
        public override StepResult Execute(Story story) {
            var.Prepare(story, context, contexts)
                .MultiplyValue();

            if (!string.IsNullOrEmpty(comment)) {
                story.ShowText(TextConfig.WithTextAndStyle($"[{comment}]", StoryTextStyle.Aside));
            }
            return StepResult.Immediate;
        }

        public override void AppendKnownEffects(Story story, ref StructList<string> effects) {
            effects.Add(LocTerms.StoryRisingCost.Translate());
        }
    }
}