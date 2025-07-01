using Awaken.TG.Main.Stories.Conditions.Core;
using Awaken.TG.Main.Stories.Core.Attributes;
using Awaken.TG.Main.Stories.Interfaces;
using Awaken.TG.Main.Stories.Quests.Templates;
using Awaken.TG.Main.Stories.Runtime;
using Awaken.TG.Main.Stories.Runtime.Nodes;
using Awaken.TG.Main.Templates;
using Awaken.Utility.Extensions;

namespace Awaken.TG.Main.Stories.Quests.Objectives {
    [Element("Quest: Check State")]
    public class CEditorQuestState : EditorCondition, IStoryQuestRef {

        [TemplateType(typeof(QuestTemplate))]
        public TemplateReference questRef;
        public QuestState requiredState;

        public TemplateReference QuestRef => questRef;
        public string TargetValue => "Is: " + requiredState.ToStringFast();

        protected override StoryCondition CreateRuntimeConditionImpl(StoryGraphParser parser) {
            return new CQuestState {
                questRef = questRef,
                requiredState = requiredState
            };
        }
    }

    public partial class CQuestState : StoryCondition {
        public TemplateReference questRef;
        public QuestState requiredState;
        
        public override bool Fulfilled(Story story, StoryStep step) {
            return QuestUtils.StateOfQuestWithId(story.Memory, questRef) == requiredState;
        }
    }
}