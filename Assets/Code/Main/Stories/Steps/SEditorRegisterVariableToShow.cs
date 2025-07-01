using System;
using Awaken.TG.Main.Stories.Core.Attributes;
using Awaken.TG.Main.Stories.Execution;
using Awaken.TG.Main.Stories.Runtime;
using Awaken.TG.Main.Stories.Runtime.Nodes;
using Awaken.TG.Main.Stories.Steps.Helpers;
using Awaken.TG.Utility.Attributes.List;
using Awaken.TG.Utility.Attributes.Tags;
using UnityEngine;

namespace Awaken.TG.Main.Stories.Steps {
    [Element("Technical/Text: Register variable to show in text")]
    public class SEditorRegisterVariableToShow : EditorStep {
        [Tags(TagsCategory.Context)] 
        public string[] context = Array.Empty<string>();
        [List(ListEditOption.Buttons)]
        public Context[] contexts = Array.Empty<Context>();

        [Tooltip("After registration you can use variable as '{[variableName]}'")]
        public string variableName;

        protected override StoryStep CreateRuntimeStepImpl(StoryGraphParser parser) {
            return new SRegisterVariableToShow {
                context = context,
                contexts = contexts,
                variableName = variableName
            };
        }
    }
    
    public partial class SRegisterVariableToShow : StoryStep {
        public string[] context = Array.Empty<string>();
        public Context[] contexts = Array.Empty<Context>();
        public string variableName;
        
        public override StepResult Execute(Story story) {
            var variableContext = StoryUtils.Context(story, context, contexts);
            var memoryValue = story.Memory.Context(variableContext).Get<float>(variableName, 0);
            story.Memory.Context(story).Set(variableName, memoryValue);
            return StepResult.Immediate;
        }
    }
}