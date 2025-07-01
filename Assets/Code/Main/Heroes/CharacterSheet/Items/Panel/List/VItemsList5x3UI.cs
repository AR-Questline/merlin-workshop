using Awaken.TG.MVC.Attributes;

namespace Awaken.TG.Main.Heroes.CharacterSheet.Items.Panel.List {
    [UsesPrefab("Items/List/" + nameof(VItemsList5x3UI))]
    public class VItemsList5x3UI : VBaseItemsListUI {
        protected override void ConfigureList() {
            MaxColumnCount = 5;
            MaxRowCount = 3;
            base.ConfigureList();
        }
    }
}