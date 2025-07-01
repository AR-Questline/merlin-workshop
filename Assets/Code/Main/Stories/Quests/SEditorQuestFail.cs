using Awaken.TG.Main.Stories.Api;
using Awaken.TG.Main.Stories.Core.Attributes;
using Awaken.TG.Main.Stories.Execution;
using Awaken.TG.Main.Stories.Interfaces;
using Awaken.TG.Main.Stories.Quests.Templates;
using Awaken.TG.Main.Stories.Runtime;
using Awaken.TG.Main.Stories.Runtime.Nodes;
using Awaken.TG.Main.Stories.Steps;
using Awaken.TG.Main.Templates;

namespace Awaken.TG.Main.Stories.Quests {
    [Element("Quests/Quest: Fail")]
    public class SEditorQuestFail : EditorStep, IStoryQuestRef {

        // === Editor properties

        [TemplateType(typeof(QuestTemplate))]
        public TemplateReference questTemplate;

        public TemplateReference QuestRef => questTemplate;
        public string TargetValue => string.Empty;

        protected override StoryStep CreateRuntimeStepImpl(StoryGraphParser parser) {
            return new SQuestFail {
                questTemplate = questTemplate
            };
        }
    }

    public partial class SQuestFail : StoryStep {
        public TemplateReference questTemplate;

        public override StepResult Execute(Story story) {
            QuestUtils.SetQuestState(questTemplate, QuestState.Failed);
            return StepResult.Immediate;
        }
    }
}