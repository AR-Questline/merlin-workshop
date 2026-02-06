using Awaken.TG.Main.Scenes.SceneConstructors;
using Awaken.TG.Main.SocialServices;
using Awaken.TG.MVC.Attributes;

namespace Awaken.TG.Main.UI.TitleScreen.Expansion {
    [UsesPrefab("TitleScreen/Expansion/" + nameof(VSarrasExpansionCardUI))]
    public class VSarrasExpansionCardUI : VExpansionCardUI {
        protected override DlcId DlcId => CommonReferences.Get.SarrasDlcId;
    }
}