using System;
using Awaken.TG.Main.Localization;
using Awaken.TG.Main.Stories.Core;
using Awaken.TG.Main.Stories.Core.Attributes;
using Awaken.TG.Main.Stories.Execution;
using Awaken.TG.Main.Stories.Runtime;
using Awaken.TG.Main.Stories.Runtime.Nodes;
using Awaken.TG.Main.Stories.Steps.Helpers;
using Awaken.Utility.Collections;
using Awaken.Utility.Times;
using Sirenix.OdinInspector;
using UnityEngine;
using XNode;

namespace Awaken.TG.Main.Stories.Steps {
    [Serializable]
    [Element("StoryStart: Interact")]
    [Node.NodeTintAttribute(0.5f, 0.7f, 0.5f)]
    public class SEditorStoryStartChoice : NodeElement<StoryStartEditorNode>, IEditorStep, IOncePer {
        
        public SingleChoice choice;
        [LabelText("Once Per")][NodeEnum]
        public TimeSpans span = TimeSpans.None;
        [HideInInspector]
        public string spanFlag;

        public SStoryStartChoice CreateRuntimeStep(StoryGraphParser parser) {
            var conditions = new StructList<StoryConditionInput>(0);
            foreach (var condition in ConditionNodes()) {
                var inputConditions = parser.GetConditions(condition);
                if (inputConditions != null) {
                    conditions.Add(new StoryConditionInput {
                        conditions = inputConditions,
                        negate = condition.IsConnectionNegated(this),
                    });
                }
            }
            
            var step =  new SStoryStartChoice {
                text = choice.text,
                span = span,
                spanFlag = spanFlag,
                targetChapter = parser.GetChapter(TargetNode() as ChapterEditorNode),
            };
            step.conditions = conditions.ToArray();
#if UNITY_EDITOR
            step.DebugInfo = DebugInfo;
#endif
            return step;
        }

        string IOncePer.SpanFlag {
            get => spanFlag;
            set => spanFlag = value;
        }
        TimeSpans IOncePer.Span => span;

        IEditorChapter IEditorStep.ContinuationChapter => Parent.ContinuationChapter;
        bool IEditorStep.MayHaveContinuation => true;
    }

    public partial class SStoryStartChoice : StoryStep, IOncePer {
        public LocString text;
        [LabelText("Once Per")][NodeEnum]
        public TimeSpans span = TimeSpans.None;
        [HideInInspector]
        public string spanFlag;
        
        public StoryChapter targetChapter;
        
        public override StepResult Execute(Story story) {
            story.Clear();
            story.JumpTo(targetChapter);
            return StepResult.Immediate;
        }

        string IOncePer.SpanFlag {
            get => spanFlag;
            set => spanFlag = value;
        }
        TimeSpans IOncePer.Span => span;
    }
}