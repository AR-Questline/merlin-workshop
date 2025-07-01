using Awaken.TG.Main.Locations;
using Awaken.TG.Main.Locations.Attachments;
using Awaken.TG.MVC.Elements;
using Awaken.Utility;

namespace Awaken.TG.Main.Fights.Factions.Crimes {
    public partial class CrimeOwnerOverride : Element<Location>, IRefreshedByAttachment<CrimeOwnerOverrideAttachment> {
        public override ushort TypeForSerialization => SavedModels.CrimeOwnerOverride;

        public CrimeOwnerTemplate CrimeOwner { get; private set; }

        public void InitFromAttachment(CrimeOwnerOverrideAttachment spec, bool isRestored) {
            CrimeOwner = spec.OwnerOverride;
        }
    }
}