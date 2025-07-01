using Awaken.TG.Main.Character;
using Awaken.TG.MVC.Elements;
using Awaken.Utility;

namespace Awaken.TG.Main.Fights.Factions.Markers {
    /// <summary>
    /// Marker for ICharacters that they are inside SafeZone and shouldn't be able to fight.
    /// </summary>
    public partial class PacifistMarker : Element<ICharacter> {
        public override ushort TypeForSerialization => SavedModels.PacifistMarker;
    }
}