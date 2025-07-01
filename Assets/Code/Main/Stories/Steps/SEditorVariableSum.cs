using System;
using Awaken.TG.Main.Stories.Api;
using Awaken.TG.Main.Stories.Core.Attributes;
using Awaken.TG.Main.Stories.Execution;
using Awaken.TG.Main.Stories.Runtime;
using Awaken.TG.Main.Stories.Runtime.Nodes;
using Awaken.TG.Main.Stories.Steps.Helpers;
using Awaken.TG.Utility.Attributes.List;
using Awaken.TG.Utility.Attributes.Tags;
using Sirenix.OdinInspector;
using UnityEngine;
using Vendor.xNode.Scripts.Attributes;

namespace Awaken.TG.Main.Stories.Steps {
    /// <summary>
    /// Sets a variable than can be checked elsewhere.
    /// </summary>
    [Element("Variables/Variable: Add to another"), NodeSupportsOdin]
    public class SEditorVariableSum : SEditorVariableReference {
        [Space, DisplayAsString, HideLabel]
        public string SecondVariableTitle = "Variable below will be added to the one above";
        
        // === Editor properties
        [HideIf(nameof(IsSecondVariableDefined))]
        [Tags(TagsCategory.Context)]
        public string[] secondContext = new string[0];

        [HideIf(nameof(IsSecondVariableDefined))]
        [List(ListEditOption.Buttons)]
        public Context[] secondContexts = new Context[0];

        [Setter]
        public Variable secondVar;
        
        protected bool IsSecondVariableDefined => secondVar.type == VariableType.Defined;
        
        public override string extraInfo => "";

        protected override StoryStep CreateRuntimeStepImpl(StoryGraphParser parser) {
            return new SVariableSum {
                context = context,
                contexts = contexts,
                var = var,
                secondContext = secondContext,
                secondContexts = secondContexts,
                secondVar = secondVar,
                comment = comment
            };
        }
    }
    
    public partial class SVariableSum : SVariableReference {
        public string[] secondContext = Array.Empty<string>();
        public Context[] secondContexts = Array.Empty<Context>();
        public Variable secondVar;
        
        public override StepResult Execute(Story story) {
            VariableHandle varHandle = var.Prepare(story, context, contexts);
            float value1 = varHandle.GetValue(0);
            float value2 = secondVar.Prepare(story, secondContext, secondContexts).GetValue(0);
            varHandle.SetValue(value1 + value2);
            if (!string.IsNullOrEmpty(comment)) {
                story.ShowText(TextConfig.WithTextAndStyle($"[{comment}]", StoryTextStyle.Aside));
            }
            return StepResult.Immediate;
        }
    }
}