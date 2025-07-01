using Awaken.TG.MVC.Elements;
using Awaken.Utility;

namespace Awaken.TG.Main.Heroes.Items {
    /// <summary>
    /// Marker script for locking item. It prevents changing item slot and also prevents dropping item.
    /// </summary>
    public partial class LockItemSlot : Element<Item> {
        public override ushort TypeForSerialization => SavedModels.LockItemSlot;
    }
}