using Awaken.TG.MVC.Attributes;
using Awaken.TG.MVC.Elements;

namespace Awaken.TG.Main.Heroes.CharacterSheet.Journal.Entries {
    [SpawnsView(typeof(VJournalButtonEntryUI))]
    public partial class JournalButtonEntryUI : Element<JournalCategoryDropdownUI> {
        public sealed override bool IsNotSaved => true;

        public IJournalEntryData Data { get; private set; }
        
        public JournalButtonEntryUI(IJournalEntryData data) {
            Data = data;
        }
    }
}