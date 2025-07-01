using Awaken.TG.Assets;
using Awaken.TG.Main.Heroes;
using Awaken.TG.Main.Locations.Attachments.Elements;
using Awaken.TG.Main.Stories.Core.Attributes;
using Awaken.TG.Main.Stories.Execution;
using Awaken.TG.Main.Stories.Runtime;
using Awaken.TG.Main.Stories.Runtime.Nodes;
using Sirenix.OdinInspector;

namespace Awaken.TG.Main.Stories.Steps {
    [Element("Hero/Teleport Hero On Hero Death: Toggle")]
    public class SEditorTeleportHeroOnHeroDeath : EditorStep {
        public bool removeElement;
        [HideIf(nameof(removeElement))] public StoryBookmark bookmark;
        [HideIf(nameof(removeElement))] public SceneReference sceneReference;
        [HideIf(nameof(removeElement))] public string sceneIndex;
        [HideIf(nameof(removeElement))] public bool discardOnSceneChange;

        protected override StoryStep CreateRuntimeStepImpl(StoryGraphParser parser) {
            return new STeleportHeroOnHeroDeath {
                removeElement = removeElement,
                bookmark = bookmark,
                sceneReference = sceneReference,
                sceneIndex = sceneIndex,
                discardOnSceneChange = discardOnSceneChange
            };
        }
    }

    public partial class STeleportHeroOnHeroDeath : StoryStep {
        public bool removeElement;
        public StoryBookmark bookmark;
        public SceneReference sceneReference;
        public string sceneIndex;
        public bool discardOnSceneChange;
        
        public override StepResult Execute(Story story) {
            if (Hero.Current.TryGetElement<TeleportHeroOnHeroDeath>(out var oldElement)) {
                oldElement.Discard();
            }
            if (removeElement) {
                return StepResult.Immediate;
            }
            
            Hero.Current.AddElement(new TeleportHeroOnHeroDeath(sceneReference, sceneIndex, bookmark, discardOnSceneChange));
            return StepResult.Immediate;
        }
    }
}