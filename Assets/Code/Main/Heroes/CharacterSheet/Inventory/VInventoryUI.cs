using Awaken.TG.Main.UI.Components.Tabs;
using Awaken.TG.MVC.Attributes;
using Awaken.TG.MVC.UI.Handlers.Focuses;

namespace Awaken.TG.Main.Heroes.CharacterSheet.Inventory {
    [UsesPrefab("CharacterSheet/Inventory/" + nameof(VInventoryUI))]
    public class VInventoryUI : VTabParent<InventoryUI>, IAutoFocusBase  { }
}