using Awaken.TG.Main.Localization;
using Awaken.TG.Main.Stories.Core;
using Awaken.TG.Main.Stories.Execution;
using Awaken.TG.Main.Stories.Runtime;
using Awaken.TG.Main.Stories.Runtime.Nodes;
using Awaken.TG.Main.Stories.Steps.Helpers;
using Awaken.Utility;
using UnityEngine.Localization;

namespace Awaken.TG.Main.Stories.Steps {
    /// <summary>
    /// Implicit step inserted at the end of chapter automatically if the story
    /// does not offer a choice.
    /// </summary>
    public class SEditorLeave : IEditorStep {
        public IEditorChapter ContinuationChapter => null;
        public bool MayHaveContinuation => false;

        public StoryStep CreateRuntimeStepImpl(StoryGraphParser parser) {
            return new SLeave();
        }
    }

    public partial class SLeave : StoryStep {
        public override StepResult Execute(Story story) {
            var text = new LocalizedString() {
                TableReference = LocalizationHelper.DefaultTable,
                TableEntryReference = "Generic/Leave"
            };
            if (story.StoryEndRequiresInteraction) {
                story.OfferChoice(ChoiceConfig.WithData(new RuntimeChoice {text = (LocString) text.ToString(), targetChapter = null}));
            } else {
                StoryUtils.EndStory(story);
            }
            return StepResult.Immediate;
        }
    }
}