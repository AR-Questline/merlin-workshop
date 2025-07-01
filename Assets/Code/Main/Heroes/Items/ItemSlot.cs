using Awaken.TG.Main.Saving;
using Awaken.TG.MVC.Elements;
using Awaken.TG.Utility.Attributes;
using Awaken.Utility;
using Newtonsoft.Json;

namespace Awaken.TG.Main.Heroes.Items {
    /// <summary>
    /// Used to persistently store index of given item in inventory.
    /// </summary>
    public partial class ItemSlot : Element<Item> {
        public override ushort TypeForSerialization => SavedModels.ItemSlot;

        [Saved] public int Index { get; private set; }

        [JsonConstructor, UnityEngine.Scripting.Preserve]
        ItemSlot() {}

        public ItemSlot(int index) {
            Index = index;
        }

        public void AssignIndex(int index) {
            Index = index;
            ParentModel.TriggerChange();
        }
    }
}