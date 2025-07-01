using Awaken.TG.Main.Stories.Api;
using Awaken.TG.Main.Stories.Core.Attributes;
using Awaken.TG.Main.Stories.Execution;
using Awaken.TG.Main.Stories.Interfaces;
using Awaken.TG.Main.Stories.Quests.Templates;
using Awaken.TG.Main.Stories.Runtime;
using Awaken.TG.Main.Stories.Runtime.Nodes;
using Awaken.TG.Main.Stories.Steps;
using Awaken.TG.Main.Templates;
using Awaken.TG.MVC;
using Awaken.Utility.Debugging;

namespace Awaken.TG.Main.Stories.Quests {
    [Element("Quests/Quest: Complete")]
    public class SEditorQuestComplete : EditorStep, IStoryQuestRef {

        // === Editor properties

        [TemplateType(typeof(QuestTemplateBase))]
        public TemplateReference questTemplate;
        public bool completeActiveObjectives;

        public TemplateReference QuestRef => questTemplate;
        public string TargetValue => completeActiveObjectives ? "Completes Active Objectives" : string.Empty;

        protected override StoryStep CreateRuntimeStepImpl(StoryGraphParser parser) {
            return new SQuestComplete {
                questTemplate = questTemplate,
                completeActiveObjectives = completeActiveObjectives
            };
        }
    }
    
    public partial class SQuestComplete : StoryStep {
        public TemplateReference questTemplate;
        public bool completeActiveObjectives;
        
        public override StepResult Execute(Story story) {
            QuestTemplateBase template = questTemplate.Get<QuestTemplateBase>();

            if (template == null) {
                Log.Important?.Error($"Null quest template assigned in story: {story.ID}, guid: {story.Guid}");
                return StepResult.Immediate;
            }
            
            QuestState currentState = QuestUtils.StateOfQuestWithId(story.Memory, questTemplate);
            
            if (currentState == QuestState.NotTaken) {
                // Auto start quests before completing them
                Quest quest = new(template);
                World.Add(quest);
                currentState = quest.State;
            } 
            
            if (currentState == QuestState.Active) {
                QuestUtils.Complete(questTemplate, completeActiveObjectives);
            }
            
            return StepResult.Immediate;
        }
    }
}