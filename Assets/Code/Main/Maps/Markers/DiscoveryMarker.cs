using Awaken.TG.Main.Locations;
using Awaken.TG.Main.Locations.Discovery;
using Awaken.TG.MVC;
using Awaken.Utility;
using Newtonsoft.Json;

namespace Awaken.TG.Main.Maps.Markers {
    public partial class DiscoveryMarker : LocationMarker {
        public override ushort TypeForSerialization => SavedModels.DiscoveryMarker;

        public DiscoveryMarkerData DiscoveryMarkerData => MarkerData as DiscoveryMarkerData;
        LocationDiscovery LocationDiscovery => ParentModel.Element<LocationDiscovery>();
        protected override bool IsVisibleUnderFogOfWar => true;

        protected override void OnInitialize() {
            base.OnInitialize();
            ParentModel.ListenTo(Location.Events.LocationCleared, UpdateIcon, this);
            LocationDiscovery.ListenTo(LocationDiscovery.Events.LocationDiscovered, UpdateIcon, this);
            LocationDiscovery.ListenTo(LocationDiscovery.Events.LocationEntered, _ => SetEnabled(false), this);
            LocationDiscovery.ListenTo(LocationDiscovery.Events.LocationExited, _ => SetEnabled(true), this);
            ParentModel.OnVisualLoaded(_ => UpdateIcon());
        }

        void UpdateIcon() {
            bool isCleared = ParentModel.Cleared;
            bool isDiscovered = ParentModel.Element<LocationDiscovery>().Discovered;
            var data = DiscoveryMarkerData;
            SetIcon((isCleared, isDiscovered) switch {
                (true, _) => data.InactiveMarkerIcon,
                (_, true) => data.MarkerIcon,
                _ => data.UndiscoveredMarkerIcon
            });
            
            CompassElement.UpdateIcon();
        }
    }
}