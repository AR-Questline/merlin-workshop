using Awaken.TG.Main.Heroes.CharacterSheet.Journal.Tabs;

namespace Awaken.TG.Main.UI.HUD.AdvancedNotifications.LeftScreen.Journal {
    public partial class JournalUnlockNotification : AdvancedNotification {
        public readonly string journalEntry;
        public readonly JournalSubTabType journalTabType;

        public JournalUnlockNotification(string journalEntry, JournalSubTabType journalTabType) {
            this.journalEntry = journalEntry;
            this.journalTabType = journalTabType;
        }
    }
}