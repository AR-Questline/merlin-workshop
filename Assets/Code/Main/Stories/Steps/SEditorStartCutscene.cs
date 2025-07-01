using Awaken.TG.Graphics.Cutscenes;
using Awaken.TG.Main.Scenes.SceneConstructors;
using Awaken.TG.Main.Stories.Core.Attributes;
using Awaken.TG.Main.Stories.Execution;
using Awaken.TG.Main.Stories.Runtime;
using Awaken.TG.Main.Stories.Runtime.Nodes;
using Awaken.TG.Main.Templates;
using Awaken.TG.MVC;

namespace Awaken.TG.Main.Stories.Steps {
    [Element("Cutscenes/Cutscene: Start")]
    public class SEditorStartCutscene : EditorStep {
        public bool discardView = true;
        public bool takeAwayControl = true;
        public float toBlackDuration;
        public bool isTriggeringPortalOnExit;
        [TemplateType(typeof(CutsceneTemplate))]
        public TemplateReference cutscene;

        protected override StoryStep CreateRuntimeStepImpl(StoryGraphParser parser) {
            return new SStartCutscene {
                toBlackDuration = toBlackDuration,
                discardView = discardView,
                takeAwayControl = takeAwayControl,
                isTriggeringPortalOnExit = isTriggeringPortalOnExit,
                cutscene = cutscene
            };
        }
    }

    public partial class SStartCutscene : StoryStep {
        public float toBlackDuration;
        public bool discardView = true;
        public bool takeAwayControl = true;
        public bool isTriggeringPortalOnExit;
        public TemplateReference cutscene;
        
        public override StepResult Execute(Story story) {
            if (DebugReferences.FastStory) {
                return StepResult.Immediate;
            }
            
            if (DebugReferences.ImmediateStory) {
                return StepResult.Immediate;
            }

            if (discardView) {
                story.RemoveView();
            }
            var result = new StepResult();
            Cutscene cut = new(cutscene.Get<CutsceneTemplate>(), result, toBlackDuration, isTriggeringPortalOnExit, takeAwayControl);
            World.Add(cut);
            return result;
        }
    }
}