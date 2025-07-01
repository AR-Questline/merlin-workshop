using Awaken.TG.Main.Stories.Core;
using Awaken.TG.Main.Stories.Core.Attributes;
using Awaken.TG.Main.Stories.Execution;
using Awaken.TG.Main.Stories.Runtime;
using Awaken.TG.Main.Stories.Runtime.Nodes;
using Awaken.TG.Main.Stories.Steps.Helpers;
using Awaken.Utility.Times;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Awaken.TG.Main.Stories.Steps {
    [Element("Branch/Branch: Node Jump")]
    public class SEditorNodeJump : EditorStep, IOncePer {
        
        // IOncePer implementation
        [HideInInspector]
        public string spanFlag;
        [LabelText("Once Per")][NodeEnum]
        public TimeSpans span = TimeSpans.None;

        protected override StoryStep CreateRuntimeStepImpl(StoryGraphParser parser) {
            return new SNodeJump {
                spanFlag = spanFlag,
                span = span,
                targetChapter = parser.GetChapter(TargetNode() as ChapterEditorNode),
            };
        }

        string IOncePer.SpanFlag {
            get => spanFlag;
            set => spanFlag = value;
        }
        TimeSpans IOncePer.Span => span;
    }
    
    public partial class SNodeJump : StoryStep, IOncePer {
        public string spanFlag;
        public TimeSpans span = TimeSpans.None;
        
        public StoryChapter targetChapter;
        
        public override StepResult Execute(Story story) {
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