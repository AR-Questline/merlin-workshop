using Awaken.TG.Main.Stories.Choices;
using Awaken.TG.Main.Stories.Core;
using Awaken.TG.Main.Stories.Core.Attributes;
using Awaken.TG.Main.Stories.Runtime;
using Awaken.TG.Main.Stories.Runtime.Nodes;

namespace Awaken.TG.Main.Stories.Steps {
    /// <summary>
    /// One step that supports adding multiple choices - mainly for ease of use.
    /// </summary>
    [Element("Branch/Branch: Choice Exit")]
    public class SEditorChoicesExit : SEditorChoice {
        public bool hiddenFromPlayer;

        protected override StoryStep CreateRuntimeStepImpl(StoryGraphParser parser) {
            return new SChoiceExit {
                hiddenFromPlayer = hiddenFromPlayer,
                
                choice = choice.ToRuntimeChoice(parser),
                audioClip = audioClip,
                spanFlag = spanFlag,
                span = span,
                choiceIcon = choiceIcon,
                targetChapter = parser.GetChapter(TargetNode() as ChapterEditorNode),
            };
        }
    }

    public partial class SChoiceExit : SChoice {
        public bool hiddenFromPlayer;
        
        public override bool ShouldBeAvailable(Story story) {
            return (!hiddenFromPlayer && base.ShouldBeAvailable(story)) || ShouldExecuteInstantly(story);
        }

        protected override bool ShouldExecuteInstantly(Story story) {
            return base.ShouldExecuteInstantly(story) || story.Elements<Choice>().IsEmpty();
        }
    }
}