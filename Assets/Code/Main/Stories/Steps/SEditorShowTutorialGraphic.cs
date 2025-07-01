using Awaken.TG.Assets;
using Awaken.TG.Main.Stories.Core.Attributes;
using Awaken.TG.Main.Stories.Runtime;
using Awaken.TG.Main.Stories.Runtime.Nodes;
using Awaken.TG.Main.Tutorials;
using Awaken.TG.Main.Tutorials.TutorialPopups;
using Awaken.TG.MVC;
using UnityEngine;

namespace Awaken.TG.Main.Stories.Steps {
    [Element("Technical/Tutorial: Show Graphic")]
    public class SEditorShowTutorialGraphic : SEditorShowTutorialText {
        [ARAssetReferenceSettings(new[] {typeof(Texture2D), typeof(Sprite)}, true, AddressableGroup.UI)]
        public ShareableSpriteReference handle;

        protected override StoryStep CreateRuntimeStepImpl(StoryGraphParser parser) {
            return new SShowTutorialGraphic {
                handle = handle,
                title = title,
                text = text
            };
        }
    }

    public partial class SShowTutorialGraphic : SShowTutorialText {
        public ShareableSpriteReference handle;
        
        protected override Model Show() => TutorialGraphic.Show(new TutorialConfig.GraphicTutorial {
            graphic = handle, 
            title = title, 
            text = text
        });
    }
}