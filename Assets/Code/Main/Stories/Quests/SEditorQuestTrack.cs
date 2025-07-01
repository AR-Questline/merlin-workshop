using System.Linq;
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
    [Element("Quests/Quest: Track")]
    public class SEditorQuestTrack : EditorStep, IStoryQuestRef {

        // === Editor properties

        [TemplateType(typeof(QuestTemplate))]
        public TemplateReference questRef;
        
        public TemplateReference QuestRef => questRef;
        public string TargetValue => string.Empty;

        protected override StoryStep CreateRuntimeStepImpl(StoryGraphParser parser) {
            return new SQuestTrack {
                questRef = questRef
            };
        }
    }

    public partial class SQuestTrack : StoryStep {
        public TemplateReference questRef;
        
        public override StepResult Execute(Story story) {
            QuestTemplate template = questRef.Get<QuestTemplate>();
            var quest = World.All<Quest>().FirstOrDefault(q => q.Template == template);
            if (quest != null) {
                World.Only<QuestTracker>().Track(quest);
            } else {
                Log.Important?.Error($"Didn't found any quest with template: {template.displayName}");
            }
            return StepResult.Immediate;
        }
    }
}