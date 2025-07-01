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
using Sirenix.OdinInspector;
using UnityEngine;
using Vendor.xNode.Scripts.Attributes;

namespace Awaken.TG.Main.Stories.Quests {
    [Element("Quests/Quest: Add"), NodeSupportsOdin]
    public class SEditorQuestAdd : EditorStep, IStoryQuestRef {

        // === Editor properties

        [TemplateType(typeof(QuestTemplate)), HideLabel]
        public TemplateReference questRef;
        
        public TemplateReference QuestRef => questRef;
        public string TargetValue => string.Empty;

        protected override StoryStep CreateRuntimeStepImpl(StoryGraphParser parser) {
            return new SQuestAdd {
                questRef = questRef
            };
        }
    }

    public partial class SQuestAdd : StoryStep {
        public TemplateReference questRef;
        
        public override StepResult Execute(Story story) {
            QuestTemplate template = questRef.Get<QuestTemplate>();

            if (template == null) {
                Log.Critical?.Error($"Null quest template assigned in story: {story.ID}, guid: {story.Guid}");
                return StepResult.Immediate;
            }
            
            bool alreadyTaken = QuestUtils.AlreadyTaken(story.Memory, questRef);
            if (alreadyTaken) {
                Log.Important?.Warning($"Quest ({template.GUID} {template.gameObject.name}) is already in progress/completed (story: {story.ID})");
                return StepResult.Immediate;
            }

            Quest quest = new(template);
            World.Add(quest);
            return StepResult.Immediate;
        }
    }
}