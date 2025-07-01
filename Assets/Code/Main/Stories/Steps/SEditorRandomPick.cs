using Awaken.TG.Code.Utility;
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

namespace Awaken.TG.Main.Stories.Steps {
    /// <summary>
    /// Picks randomly between several possible chapters.
    /// </summary>
    [Element("Branch/Branch: Random")]
    public class SEditorRandomPick : EditorStep, IOncePer {
        public int weight = 1;
        
        // IOncePer implementation
        [HideInInspector]
        public string spanFlag;
        [LabelText("Once Per")][NodeEnum]
        public TimeSpans span = TimeSpans.None;

        protected override StoryStep CreateRuntimeStepImpl(StoryGraphParser parser) {
            return new SRandomPick {
                weight = weight,
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

    public partial class SRandomPick : StoryStep, IOncePer {
        public int weight = 1;
        
        public string spanFlag;
        public TimeSpans span = TimeSpans.None;
        
        public StoryChapter targetChapter;

        public SRandomPick() {
            AutoPerformed = false;
        }
        
        public override StepResult Execute(Story story) {
            var validPicks = ValidPicks(story, parentChapter);
            var index = RandomUtil.WeightedSelect(0, validPicks.Count - 1, (i) => validPicks[i].weight);
            return validPicks[index].PerformJump(story);
        }

        public StepResult PerformJump(Story story) {
            story.JumpTo(targetChapter);
            StoryUtilsRuntime.StepPerformed(story, this);
            return StepResult.Immediate;
        }

        string IOncePer.SpanFlag {
            get => spanFlag;
            set => spanFlag = value;
        }
        TimeSpans IOncePer.Span => span;
        
        public static StructList<SRandomPick> ValidPicks(Story story, StoryChapter chapter) {
            var list = new StructList<SRandomPick>(0);
            foreach (var step in chapter.steps) {
                if (step is SRandomPick pick) {
                    if (StoryUtilsRuntime.ShouldExecute(story, pick)) {
                        list.Add(pick);
                    }
                }
            }
            return list;
        }
    }
}
