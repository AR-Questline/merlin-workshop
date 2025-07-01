using Awaken.TG.MVC.Attributes;
using Awaken.TG.MVC.Elements;

namespace Awaken.TG.Main.Stories.Choices.ChoicePreviews {
    [SpawnsView(typeof(VBonusPreview))]
    public partial class BonusPreview : Element<ChoicePreview> {
        public sealed override bool IsNotSaved => true;

        public readonly IHoverInfo bonusInfo;
        
        public BonusPreview(IHoverInfo bonusInfo) {
            this.bonusInfo = bonusInfo;
        }
    }
}