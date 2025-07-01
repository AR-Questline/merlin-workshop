using Awaken.TG.Main.Stories.Api;
using Awaken.TG.Main.Stories.Core.Attributes;
using Awaken.TG.Main.Stories.Execution;
using Awaken.TG.Main.Stories.Interfaces;
using Awaken.TG.Main.Stories.Quests.Templates;
using Awaken.TG.Main.Stories.Runtime;
using Awaken.TG.Main.Stories.Runtime.Nodes;
using Awaken.TG.Main.Stories.Steps;
using Awaken.TG.Main.Templates;
using Awaken.Utility.Extensions;
using Sirenix.OdinInspector;
using Vendor.xNode.Scripts.Attributes;

namespace Awaken.TG.Main.Stories.Quests.Objectives {
    [Element("Quests/Quest: Change Objective State")]
    public class SEditorObjectiveChange : EditorStep, IStoryQuestRef {
        [TemplateType(typeof(QuestTemplateBase)), HideLabel]
        public TemplateReference questRef;
        [HideInStoryGraph]
        public string objectiveGuid;
        [HideLabel]
        public ObjectiveState newState;
        [InfoBox("Allow change from Failed or Complete states, use it wisely")]
        public bool allowChangeFromFinalStates;

        public TemplateReference QuestRef => questRef;
        public string TargetValue {
            get {
                var result = "-> " + newState.ToStringFast();
#if UNITY_EDITOR
                if (questRef.TryGet<QuestTemplateBase>() != null) {
                    result = questRef.TryGet<QuestTemplateBase>().Editor_GetNameOfObjectiveSpec(objectiveGuid) + " " + result;
                } else {
                    result = "Null" + " " + result;
                }
#endif
                return result;
            }
        }

        protected override StoryStep CreateRuntimeStepImpl(StoryGraphParser parser) {
            return new SObjectiveChange {
                questRef = questRef,
                objectiveGuid = objectiveGuid,
                newState = newState,
                allowChangeFromFinalStates = allowChangeFromFinalStates,
            };
        }
    }
    
    public partial class SObjectiveChange : StoryStep {
        public TemplateReference questRef;
        public string objectiveGuid;
        public ObjectiveState newState;
        public bool allowChangeFromFinalStates;
        
        public override StepResult Execute(Story story) {
            QuestUtils.ChangeObjectiveState(questRef, objectiveGuid, newState, allowChangeFromFinalStates);
            return StepResult.Immediate;
        }
    }
}