using Awaken.TG.MVC;
using Awaken.TG.MVC.Elements;

namespace Awaken.TG.Main.UI.TitleScreen.Expansion {
    public class ExpansionEntryUI : Element<IModel> {
        public override bool IsNotSaved => true;
        
        public ExpansionEntryData ExpansionEntryData { get; }
        public int ExpansionIndex => ExpansionEntryData.expansionIndex;

        public ExpansionEntryUI(ExpansionEntryData expansionEntryData) {
            ExpansionEntryData = expansionEntryData;
        }
    }
}