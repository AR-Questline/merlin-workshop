using Awaken.TG.Main.Stories.Api;
using Awaken.TG.Main.Stories.Core.Attributes;
using Awaken.TG.Main.Stories.Execution;
using Awaken.TG.Main.Stories.Steps.Helpers;
using Awaken.TG.Utility.Attributes.List;
using Awaken.TG.Utility.Attributes.Tags;
using Sirenix.OdinInspector;
using System;
using Awaken.TG.Main.Stories.Runtime;
using Awaken.TG.Main.Stories.Runtime.Nodes;

namespace Awaken.TG.Main.Stories.Steps {
    [Element("Variables/Variable: Copy")]
    public class SEditorVariableCopy : SEditorVariableReference {
        
        // From Variable
        [HideIf(nameof(IsFromVariableDefined))]
        [Tags(TagsCategory.Context)]
        public string[] contextFrom = Array.Empty<string>();

        [HideIf(nameof(IsFromVariableDefined))]
        [List(ListEditOption.Buttons)]
        public Context[] contextsFrom = Array.Empty<Context>();

        [Getter]
        public Variable variableFrom;

        bool IsFromVariableDefined => variableFrom.type == VariableType.Defined;
        public override string extraInfo => $"Copy from {variableFrom.Label()}";

        protected override StoryStep CreateRuntimeStepImpl(StoryGraphParser parser) {
            return new SVariableCopy {
                context = context,
                contexts = contexts,
                var = var,
                comment = comment,
                contextFrom = contextFrom,
                contextsFrom = contextsFrom,
                variableFrom = variableFrom
            };
        }
    }
    
    public partial class SVariableCopy : SVariableReference {
        public string[] contextFrom = Array.Empty<string>();
        public Context[] contextsFrom = Array.Empty<Context>();
        public Variable variableFrom;
        
        public override StepResult Execute(Story story) {
            var from = variableFrom.GetValue(story, contextFrom, contextsFrom);
            var.Prepare(story, context, contexts).SetValue(from);
            if (!string.IsNullOrEmpty(comment)) {
                story.ShowText(TextConfig.WithTextAndStyle($"[{comment}]", StoryTextStyle.Aside));
            }
            return StepResult.Immediate;
        }
    }
}