using Awaken.TG.Main.Heroes.Statuses;
using Awaken.TG.Main.Heroes.Statuses.Duration;
using Awaken.TG.Main.Skills;
using Awaken.TG.Main.Stories.Actors;
using Awaken.TG.Main.Stories.Core.Attributes;
using Awaken.TG.Main.Stories.Execution;
using Awaken.TG.Main.Stories.Interfaces;
using Awaken.TG.Main.Stories.Runtime;
using Awaken.TG.Main.Stories.Runtime.Nodes;
using Awaken.TG.Main.Stories.Steps.Helpers;
using Awaken.TG.Main.Templates;
using Sirenix.OdinInspector;

namespace Awaken.TG.Main.Stories.Steps {
    [Element("Hero/Hero: Give status")]
    public class SEditorGiveStatus : EditorStep, IStoryActorRef {
        [TemplateType(typeof(StatusTemplate))]
        public TemplateReference statusReference;

        public bool showStatusPopup;
        [ShowIf(nameof(showStatusPopup))]
        public ActorRef statusGiver;
        public bool overrideDuration;
        public float newDuration;
        
        public ActorRef[] ActorRef => new[] { statusGiver };

        protected override StoryStep CreateRuntimeStepImpl(StoryGraphParser parser) {
            return new SGiveStatus {
                statusReference = statusReference,
                showStatusPopup = showStatusPopup,
                statusGiver = statusGiver,
                overrideDuration = overrideDuration,
                newDuration = newDuration
            };
        }
    }

    public partial class SGiveStatus : StoryStep {
        public TemplateReference statusReference;

        public bool showStatusPopup;
        public ActorRef statusGiver;
        public bool overrideDuration;
        public float newDuration;
        
        public override StepResult Execute(Story story) {
            var statusTemplate = statusReference.Get<StatusTemplate>();
            
            var sourceInfo = StatusSourceInfo.FromStatus(statusTemplate);
            //Status popup is shown only if the status was applied by no one or not by Hero
            if (showStatusPopup) {
                if (StoryUtils.FindCharacter(story, statusGiver.Get()) is { } character) {
                    sourceInfo.WithCharacter(character);
                }
            } else {
                sourceInfo.WithCharacter(story.Hero);
            }

            if (overrideDuration) {
                story.Hero.Statuses.AddStatus(statusTemplate, sourceInfo, new TimeDuration(newDuration));
            } else {
                story.Hero.Statuses.AddStatus(statusTemplate, sourceInfo);
            }
            return StepResult.Immediate;
        }
    }
}