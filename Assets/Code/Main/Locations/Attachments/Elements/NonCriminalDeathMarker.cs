using Awaken.TG.MVC.Elements;
using Awaken.Utility;

namespace Awaken.TG.Main.Locations.Attachments.Elements {
    /// <summary>
    /// Marker element for blocking NPCs from having Corpse added to them.
    /// Corpse constantly alerts other NPCs around about their death.
    /// </summary>
    public partial class NonCriminalDeathMarker : Element<Location> {
        public override ushort TypeForSerialization => SavedModels.NonCriminalDeathMarker;
    }
}
