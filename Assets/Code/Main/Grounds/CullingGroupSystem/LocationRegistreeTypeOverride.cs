using Awaken.TG.Main.Grounds.CullingGroupSystem.CullingGroups;
using Awaken.TG.Main.Locations;
using Awaken.TG.Main.Locations.Attachments;
using Awaken.TG.MVC.Elements;
using Awaken.Utility;

namespace Awaken.TG.Main.Grounds.CullingGroupSystem {
    public partial class LocationRegistreeTypeOverride : Element<Location>, IRefreshedByAttachment<LocationCullingOverrideAttachment> {
        public override ushort TypeForSerialization => SavedModels.LocationRegistreeTypeOverride;

        LocationRegistreeType _registreeType;
        
        public void InitFromAttachment(LocationCullingOverrideAttachment spec, bool isRestored) {
            _registreeType = spec.TypeOverride;
        }

        public Registree GetRegistree(Location owner) =>
            _registreeType switch {
                LocationRegistreeType.Large => Registree.ConstructFor<LargeLocationCullingGroup>(owner).Build(),
                _ => Registree.ConstructFor<LocationCullingGroup>(owner).Build(),
            };
    }
}