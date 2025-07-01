using Awaken.TG.Main.Locations;
using Awaken.TG.MVC.Elements;

namespace Awaken.TG.Main.Character {
    /// <summary>
    /// Marker element for hiding health bar.
    /// </summary>
    public partial class HideHealthBar : Element<Location> {
        public sealed override bool IsNotSaved => true;
    }
}