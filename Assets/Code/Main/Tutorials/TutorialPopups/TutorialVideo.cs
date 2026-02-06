using Awaken.TG.Main.Utility.Video;
using Awaken.TG.MVC;

namespace Awaken.TG.Main.Tutorials.TutorialPopups {
    public partial class TutorialVideo : TutorialText {
        public LoadingHandle Handle { get; }

        public TutorialVideo(LoadingHandle loadingHandle, TutorialConfig.VideoTutorial dataOwner, bool disableOtherCanvases, ViewContext viewContext) : base(dataOwner, disableOtherCanvases, viewContext) {
            Handle = loadingHandle;
        }

        public static TutorialVideo Show(TutorialConfig.VideoTutorial dataOwner, bool disableOtherCanvases = true, ViewContext viewContext = ViewContext.Gameplay) {
            TutorialVideo tutorial = World.Add(new TutorialVideo(dataOwner.video, dataOwner, disableOtherCanvases, viewContext));
            return TryShow(tutorial, typeof(VTutorialVideo));
        }

        public void InitVideo() {
            if (HasBeenDiscarded) return;
            
            Video video = Video.Custom(View<VTutorialVideo>(), Handle, new Video.Config {
                allowSkip = false,
                hideCursor = false,
                loop = true,
            }, this);
            World.Add(video);
        }
    }
}