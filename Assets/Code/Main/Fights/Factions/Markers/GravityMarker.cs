using Awaken.TG.Main.Character;
using Awaken.TG.Main.Locations;
using Awaken.TG.Main.Saving;
using Awaken.TG.MVC.Elements;

namespace Awaken.TG.Main.Fights.Factions.Markers {
    /// <summary>
    /// Marker for ICharacters that they are inside Gravity Changing zone and should have other gravity.
    /// </summary>
    public partial class GravityMarker : Element<ICharacter> {
        public sealed override bool IsNotSaved => true;

        public GravityChangeZone Zone { get; }

        public GravityMarker(GravityChangeZone zone) {
            this.Zone = zone;
        }
    }
}