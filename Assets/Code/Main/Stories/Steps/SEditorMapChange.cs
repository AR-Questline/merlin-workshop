using Awaken.TG.Assets;
using Awaken.TG.Main.Locations.Attachments.Elements;
using Awaken.TG.Main.Stories.Core.Attributes;
using Awaken.TG.Main.Stories.Execution;
using Awaken.TG.Main.Stories.Runtime;
using Awaken.TG.Main.Stories.Runtime.Nodes;
using Awaken.TG.MVC;
using Awaken.TG.MVC.Domains;

namespace Awaken.TG.Main.Stories.Steps {
    [Element("Location/Location: Map Change")]
    public class SEditorMapChange : EditorStep {
        public SceneReference scene;
        public string indexTag;
        public bool useDefaultPortal;

        protected override StoryStep CreateRuntimeStepImpl(StoryGraphParser parser) {
            return new SMapChange {
                scene = scene,
                indexTag = indexTag,
                useDefaultPortal = useDefaultPortal
            };
        }
    }

    public partial class SMapChange : StoryStep {
        public SceneReference scene;
        public string indexTag;
        public bool useDefaultPortal;
        
        public override StepResult Execute(Story story) {
            Portal.MapChangeTo(story.Hero, scene, World.Services.Get<SceneService>().ActiveSceneRef, useDefaultPortal ? null : indexTag);
            return StepResult.Immediate;
        }
    }
}