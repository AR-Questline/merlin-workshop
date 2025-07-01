using Awaken.TG.Main.Heroes.Items;
using Awaken.TG.MVC.Elements;
using Awaken.Utility;

namespace Awaken.TG.Main.Locations.Containers {
    /// <summary> Marker element for item that cannot be picked from another inventory </summary>
    public abstract partial class NonPickable : Element<Item> { }

    public partial class NonPickpocketable : NonPickable {
        public override ushort TypeForSerialization => SavedModels.NonPickpocketable;
    }
}