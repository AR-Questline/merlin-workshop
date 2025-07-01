using Awaken.TG.MVC;
using Awaken.TG.MVC.Attributes;
using Awaken.TG.MVC.Domains;
using Awaken.Utility;

namespace Awaken.TG.Debugging {
    [SpawnsView(typeof(VDebugLocation))]
    public partial class DebugLocation : Model {
        public override ushort TypeForSerialization => SavedModels.DebugLocation;

        public override Domain DefaultDomain => Domain.Gameplay;
    }
}