using Awaken.TG.Assets;
using Awaken.TG.Main.Heroes;
using Awaken.TG.MVC;

namespace Awaken.TG.Main.Tutorials.TutorialPopups {
    public partial class TutorialGraphic : TutorialText {
        public ShareableSpriteReference SpriteReference { get; }

        public TutorialGraphic(ShareableSpriteReference spriteReference, string titleText, string contentText, bool disableOtherCanvases, ViewContext viewContext) : base(titleText, contentText, disableOtherCanvases, viewContext) {
            SpriteReference = spriteReference;
        }

        public static TutorialGraphic Show(TutorialConfig.GraphicTutorial dataOwner, bool disableOtherCanvases = true, ViewContext viewContext = ViewContext.Gameplay) {
            string textToDisplay = dataOwner.GetTranslatedText();
            TutorialGraphic tutorial = World.Add(new TutorialGraphic(dataOwner.graphic, dataOwner.title, textToDisplay, disableOtherCanvases, viewContext));
            return TryShow(tutorial, typeof(VTutorialGraphic));
        }
    }
}