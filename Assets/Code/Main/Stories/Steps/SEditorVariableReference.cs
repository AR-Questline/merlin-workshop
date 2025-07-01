using Awaken.TG.Main.Stories.Api;
using Awaken.TG.Main.Stories.Core.Attributes;
using Awaken.TG.Main.Stories.Execution;
using Awaken.TG.Main.Stories.Steps.Helpers;
using Awaken.TG.Utility.Attributes.List;
using Awaken.TG.Utility.Attributes.Tags;
using Sirenix.OdinInspector;
using System;
using System.Collections.Generic;
using Awaken.TG.Main.Stories.Interfaces;
using Awaken.TG.Main.Stories.Runtime;
using Awaken.TG.Main.Stories.Runtime.Nodes;

namespace Awaken.TG.Main.Stories.Steps {
    [Element("Variables/Variable: Only Reference")]
    public class SEditorVariableReference : EditorStep, IStoryVariableRef {
        // === Editor properties
        [HideIf(nameof(IsVariableDefined))]
        [Tags(TagsCategory.Context)]
        public string[] context = Array.Empty<string>();

        [HideIf(nameof(IsVariableDefined))]
        [List(ListEditOption.Buttons)]
        public Context[] contexts = Array.Empty<Context>();

        [Setter]
        public Variable var;
        
        public string comment = "";
        
        protected bool IsVariableDefined => var.type == VariableType.Defined;

        // === Implementation

        IEnumerable<Variable> IStoryVariableRef.variables {
            get {
                yield return var;
            }
        }
        Context[] IStoryVariableRef.optionalContext => contexts;
        public virtual string extraInfo => "Reference Only";

        protected override StoryStep CreateRuntimeStepImpl(StoryGraphParser parser) {
            return new SVariableReference {
                context = context,
                contexts = contexts,
                var = var,
                comment = comment,
            };
        }
    }
    
    public partial class SVariableReference : StoryStep {
        public string[] context = Array.Empty<string>();
        public Context[] contexts = Array.Empty<Context>();
        public Variable var;
        public string comment;
        
        public override StepResult Execute(Story story) {
            return StepResult.Immediate;
        }
    }
}