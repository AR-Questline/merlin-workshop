using Awaken.TG.Assets;
using Awaken.TG.Main.Heroes;
using Awaken.TG.Main.Locations;
using Awaken.TG.Main.Maps.Markers;

namespace Awaken.TG.Main.UI.HUD.AdvancedNotifications.LeftScreen.LocationDiscovery {
    public readonly struct NewLocationNotificationData {
        public readonly string discoveryTitle;
        public readonly string discoveryMessage;
        public readonly int expReward;
        public readonly ShareableSpriteReference iconRef;

        public NewLocationNotificationData(Location location, string discoveryTitle, string discoveryMessage, float expMultiplier, ShareableSpriteReference iconRef = null) {
            this.discoveryTitle = discoveryTitle;
            this.discoveryMessage = discoveryMessage;
            this.expReward = Hero.Current.Development.CalculateIncomingExpReward(expMultiplier);
            this.iconRef = iconRef is { IsSet: true }
                ? iconRef
                : location.TryGetElement<LocationMarker>()?.MarkerData.MarkerIcon;
        }
    }
}