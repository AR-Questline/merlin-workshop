using Awaken.TG.Main.Heroes.CharacterSheet.Journal.Tabs;
using Awaken.TG.MVC.Elements;

namespace Awaken.TG.Main.UI.HUD.AdvancedNotifications.LeftScreen.Journal {
    public partial class JournalUnlockNotification : Element<JournalUnlockNotificationBuffer>, IAdvancedNotification {
        public sealed override bool IsNotSaved => true;

        public readonly string journalEntry;
        public readonly JournalSubTabType journalTabType;

        public JournalUnlockNotification(string journalEntry, JournalSubTabType journalTabType) {
            this.journalEntry = journalEntry;
            this.journalTabType = journalTabType;
        }
    }
}