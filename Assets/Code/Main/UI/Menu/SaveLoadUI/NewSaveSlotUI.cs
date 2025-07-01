using Awaken.TG.MVC.Attributes;
using Awaken.TG.MVC.Elements;

namespace Awaken.TG.Main.UI.Menu.SaveLoadUI {
    [SpawnsView(typeof(VNewSaveSlotUI))]
    public partial class NewSaveSlotUI : Element<SaveMenuUI>, ISaveLoadSlotUI {
        public sealed override bool IsNotSaved => true;
        public int Index => -1;
    }
}