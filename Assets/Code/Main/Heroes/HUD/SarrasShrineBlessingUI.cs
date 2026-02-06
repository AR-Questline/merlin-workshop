using Awaken.TG.MVC.Attributes;
using Awaken.TG.MVC.Elements;

namespace Awaken.TG.Main.Heroes.HUD {
    [SpawnsView(typeof(VSarrasShrineBlessingUI))]
    public partial class SarrasShrineBlessingUI : Element<UI.HUD.HUD> {
        public override bool IsNotSaved => true;
    }
}