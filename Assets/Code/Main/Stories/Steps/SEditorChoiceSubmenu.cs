using Awaken.TG.Main.Stories.Core;
using Awaken.TG.Main.Stories.Core.Attributes;
using Awaken.TG.Main.Stories.Runtime;
using Awaken.TG.Main.Stories.Runtime.Nodes;

namespace Awaken.TG.Main.Stories.Steps {
    /// <summary>
    /// One step that supports adding multiple choices - mainly for ease of use.
    /// </summary>
    [Element("Branch/Branch: Submenu")]
    public class SEditorChoiceSubmenu : SEditorChoice {
        protected override StoryStep CreateRuntimeStepImpl(StoryGraphParser parser) {
            return new SChoiceSubmenu {
                choice = choice.ToRuntimeChoice(parser),
                audioClip = audioClip,
                spanFlag = spanFlag,
                span = span,
                choiceIcon = choiceIcon,
                targetChapter = parser.GetChapter(TargetNode() as ChapterEditorNode),
            };
        }
    }
    
    public partial class SChoiceSubmenu : SChoice {
        public override bool ShouldBeAvailable(Story story) {
            return SearchAnyValidContinuationNode(story, targetChapter);
        }

        static bool SearchAnyValidContinuationNode(Story story, StoryChapter chapter) {
            while (chapter != null) {
                foreach (var step in chapter.steps) {
                    if (step is SChoiceExit) {
                        continue;
                    }
                    if (StoryUtilsRuntime.ShouldExecute(story, step)) {
                        return true;
                    }
                }
                chapter = chapter.continuation;
            }
            return false;
        }
    }
}