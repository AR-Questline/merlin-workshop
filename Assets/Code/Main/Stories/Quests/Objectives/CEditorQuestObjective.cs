using Awaken.TG.Main.Stories.Api;
using Awaken.TG.Main.Stories.Conditions.Core;
using Awaken.TG.Main.Stories.Core;
using Awaken.TG.Main.Stories.Core.Attributes;
using Awaken.TG.Main.Stories.Interfaces;
using Awaken.TG.Main.Stories.Quests.Templates;
using Awaken.TG.Main.Stories.Runtime;
using Awaken.TG.Main.Stories.Runtime.Nodes;
using Awaken.TG.Main.Templates;
using Awaken.Utility.Extensions;
using Sirenix.OdinInspector;

namespace Awaken.TG.Main.Stories.Quests.Objectives {
    [Element("Quest: Check Objective")]
    public class CEditorQuestObjective : EditorCondition, IStoryQuestRef {

        [TemplateType(typeof(QuestTemplate)), HideLabel]
        public TemplateReference questRef;
        public string objectiveGuid;
        public ObjectiveState requiredState;
        
        public TemplateReference QuestRef => questRef;
        public string TargetValue => "Is: " + requiredState.ToStringFast();

        protected override StoryCondition CreateRuntimeConditionImpl(StoryGraphParser parser) {
            return new CQuestObjective {
                questRef = questRef,
                objectiveGuid = objectiveGuid,
                requiredState = requiredState
            };
        }
    }
    
    public partial class CQuestObjective : StoryCondition {
        public TemplateReference questRef;
        public string objectiveGuid;
        public ObjectiveState requiredState;
        
        public override bool Fulfilled(Story story, StoryStep step) {
            ObjectiveState state = QuestUtils.StateOfObjective(story.Memory, questRef, objectiveGuid);
            return state == requiredState;
        }
    }
}