using Awaken.TG.Main.Heroes.CharacterSheet.Overviews;

namespace Awaken.TG.Main.Heroes.CharacterSheet.Journal.Tabs {
    public interface IVJournalCategoryUI : IVEntryParentUI {
        void ShowNoEntriesInfo(bool show);
    }
}