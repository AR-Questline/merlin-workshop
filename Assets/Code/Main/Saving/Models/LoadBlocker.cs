using Awaken.TG.MVC;
using Awaken.TG.MVC.Domains;
using Awaken.Utility;

namespace Awaken.TG.Main.Saving.Models {
    /// <summary>
    /// Marker model - means that World cannot be loaded. 
    /// </summary>
    public partial class LoadBlocker : Model {
        public override ushort TypeForSerialization => SavedModels.LoadBlocker;

        public override Domain DefaultDomain => Domain.Gameplay;
    }
}