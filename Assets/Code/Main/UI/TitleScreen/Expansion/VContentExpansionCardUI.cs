using Awaken.TG.Main.Scenes.SceneConstructors;
using Awaken.TG.Main.SocialServices;
using Awaken.TG.MVC.Attributes;

namespace Awaken.TG.Main.UI.TitleScreen.Expansion {
    [UsesPrefab("TitleScreen/Expansion/" + nameof(VContentExpansionCardUI))]
    public class VContentExpansionCardUI : VExpansionCardUI {
        protected override DlcId DlcId => CommonReferences.Get.ContentPackDlcId;
    }
}