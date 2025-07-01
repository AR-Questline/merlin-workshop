using Awaken.TG.Assets;
using Awaken.TG.Main.Stories.Core.Attributes;
using Awaken.TG.Main.Stories.Execution;
using Awaken.TG.Main.Stories.Runtime;
using Awaken.TG.Main.Stories.Runtime.Nodes;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Awaken.TG.Main.Stories.Steps {
    [Element("UI/Art: Change")]
    public class SEditorArt : EditorStep {
        [HideLabel, ARAssetReferenceSettings(new [] {typeof(Sprite), typeof(Texture)}, group: AddressableGroup.Stories)]
        public SpriteReference art;

        protected override StoryStep CreateRuntimeStepImpl(StoryGraphParser parser) {
            return new SArt {
                art = art
            };
        }
    }

    public partial class SArt : StoryStep {
        public SpriteReference art;
        
        public override StepResult Execute(Story story) {
            story.SetArt(art);
            return StepResult.Immediate;
        }
    }
}