using Awaken.TG.Main.Fights.Factions;
using Awaken.TG.Main.Fights.Factions.Crimes;
using Awaken.TG.Main.Locations;
using Awaken.TG.Main.Locations.Attachments;
using Awaken.TG.MVC.Elements;
using Awaken.Utility;

namespace Awaken.TG.Main.Heroes.Thievery {
    public partial class CrimeOverride : Element<Location>, IRefreshedByAttachment<CrimeOverrideAttachment> {
        public override ushort TypeForSerialization => SavedModels.CrimeOverride;

        CrimeOverrideAttachment _spec;
        
        public void InitFromAttachment(CrimeOverrideAttachment spec, bool isRestored) {
            _spec = spec;
        }

        public ref readonly CrimeArchetype Override(in CrimeArchetype archetype) {
            return ref _spec.Override(archetype);
        }
    }
}