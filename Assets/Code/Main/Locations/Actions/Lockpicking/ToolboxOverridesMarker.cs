using Awaken.TG.Main.Heroes;
using Awaken.TG.MVC.Elements;
using Awaken.TG.Main.Saving;

namespace Awaken.TG.Main.Locations.Actions.Lockpicking {
    /// <summary>
    /// Dev console hack: Removes lockpick and key requirements. Instant no tool dig out. No thievery hold duration.
    /// </summary>
    public partial class ToolboxOverridesMarker : Element<Hero> {
        public sealed override bool IsNotSaved => true;
    }
}
