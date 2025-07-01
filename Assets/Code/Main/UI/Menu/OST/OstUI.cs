using Awaken.TG.MVC;
using Awaken.TG.MVC.Attributes;
using Awaken.TG.MVC.Domains;

namespace Awaken.TG.Main.UI.Menu.OST {
    [SpawnsView(typeof(VOstUI))]
    public partial class OstUI : Model {
        public sealed override bool IsNotSaved => true;
        public override Domain DefaultDomain => Domain.Globals;
    }
}