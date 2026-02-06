using Awaken.TG.Assets;
using Awaken.TG.MVC;

namespace Awaken.TG.Main.Tutorials.TutorialPopups {
    public partial class TutorialGraphic : TutorialText {
        public ShareableSpriteReference SpriteReference { get; }

        public TutorialGraphic(ShareableSpriteReference spriteReference, TutorialConfig.GraphicTutorial dataOwner, bool disableOtherCanvases, ViewContext viewContext) : base(dataOwner, disableOtherCanvases, viewContext) {
            SpriteReference = spriteReference;
        }

        public static TutorialGraphic Show(TutorialConfig.GraphicTutorial dataOwner, bool disableOtherCanvases = true, ViewContext viewContext = ViewContext.Gameplay) {
            TutorialGraphic tutorial = World.Add(new TutorialGraphic(dataOwner.graphic, dataOwner, disableOtherCanvases, viewContext));
            return TryShow(tutorial, typeof(VTutorialGraphic));
        }
    }
}