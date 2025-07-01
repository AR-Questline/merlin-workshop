using System.Collections.Generic;
using System.Linq;
using Awaken.TG.Main.Fights.Factions;
using Awaken.TG.Main.Stories.Api;
using Awaken.TG.Main.Stories.Core.Attributes;
using Awaken.TG.Main.Stories.Execution;
using Awaken.TG.Main.Stories.Quests.Templates;
using Awaken.TG.Main.Stories.Runtime;
using Awaken.TG.Main.Stories.Runtime.Nodes;
using Awaken.TG.Main.Stories.Steps;
using Awaken.TG.Main.Templates;
using Awaken.TG.MVC;
using Awaken.Utility.Debugging;

namespace Awaken.TG.Main.Stories.Quests {
    [Element("Quests/Quest: Fail All For")]
    public class SEditorQuestFailAllFor : EditorStep {
        [TemplateType(typeof(FactionTemplate))]
        public TemplateReference faction;

        protected override StoryStep CreateRuntimeStepImpl(StoryGraphParser parser) {
            return new SQuestFailAllFor {
                faction = faction
            };
        }
    }
    
    public partial class SQuestFailAllFor : StoryStep {
        public TemplateReference faction;
        
        public override StepResult Execute(Story story) {
            var factionTemplate = faction.Get<FactionTemplate>();
            if (factionTemplate == null) {
                Log.Important?.Error("Faction template is null for story: " + (story as Story)?.Guid);
                return StepResult.Immediate;
            }
            var relatedQuests = World.Services.Get<TemplatesProvider>().GetAllOfType<QuestTemplateBase>().Where(q => q.RelatedFaction == factionTemplate);
            foreach (QuestTemplateBase quest in relatedQuests) {
                QuestUtils.SetQuestState(new TemplateReference(quest), QuestState.Failed);
            }
            return StepResult.Immediate;
        }
    }
}