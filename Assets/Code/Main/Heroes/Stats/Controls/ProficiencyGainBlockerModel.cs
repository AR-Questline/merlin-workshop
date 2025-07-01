using Awaken.TG.MVC;
using Awaken.TG.MVC.Domains;
using Awaken.Utility;

namespace Awaken.TG.Main.Heroes.Stats.Controls {
    public partial class ProficiencyGainBlockerModel : Model {
        public override ushort TypeForSerialization => SavedModels.ProficiencyGainBlockerModel;

        public override Domain DefaultDomain => Domain.CurrentScene();
    }
}