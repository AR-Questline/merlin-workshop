using Awaken.TG.Main.UI.Components.Tabs;
using Awaken.TG.MVC.Attributes;

namespace Awaken.TG.Main.Heroes.CharacterSheet.Items.Panel {
    [UsesPrefab("Items/" + nameof(VItemsUI))]
    public class VItemsUI : VTabParent<ItemsUI> { }
}