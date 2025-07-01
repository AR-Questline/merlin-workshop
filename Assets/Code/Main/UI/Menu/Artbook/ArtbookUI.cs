using Awaken.TG.MVC;
using Awaken.TG.MVC.Attributes;
using Awaken.TG.MVC.Domains;

namespace Awaken.TG.Main.UI.Menu.Artbook {
    [SpawnsView(typeof(VArtbookUI))]
    public partial class ArtbookUI : Model {
        public override bool IsNotSaved => true;
        public override Domain DefaultDomain => Domain.Globals;
    }
}