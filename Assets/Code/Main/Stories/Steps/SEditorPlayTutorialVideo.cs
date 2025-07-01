using Awaken.TG.Main.Stories.Core.Attributes;
using Awaken.TG.Main.Stories.Runtime;
using Awaken.TG.Main.Stories.Runtime.Nodes;
using Awaken.TG.Main.Tutorials;
using Awaken.TG.Main.Tutorials.TutorialPopups;
using Awaken.TG.Main.Utility.Video;
using Awaken.TG.MVC;

namespace Awaken.TG.Main.Stories.Steps {
    [Element("Technical/Tutorial: Play Video")]
    public class SEditorPlayTutorialVideo : SEditorShowTutorialText {
        public LoadingHandle handle;

        protected override StoryStep CreateRuntimeStepImpl(StoryGraphParser parser) {
            return new SPlayTutorialVideo {
                handle = handle,
                title = title,
                text = text
            };
        }
    }
    
    public partial class SPlayTutorialVideo : SShowTutorialText {
        public LoadingHandle handle;

        protected override Model Show() {
            return TutorialVideo.Show(new TutorialConfig.VideoTutorial {
                video = handle, 
                title = title, 
                text = text
            });
        }
    }
}