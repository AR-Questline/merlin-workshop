using Awaken.TG.MVC;
using Awaken.TG.MVC.Attributes;

namespace Awaken.TG.Main.Heroes.CharacterSheet.Inventory {
    [UsesPrefab("CharacterSheet/Inventory/" + nameof(VInventoryTabs))]
    public class VInventoryTabs : View<InventorySubTabs> { }
}
