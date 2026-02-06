using Awaken.TG.Main.Heroes.CharacterSheet.Journal.Tabs;
using Awaken.TG.Main.Heroes.CharacterSheet.Tabs;
using Awaken.TG.Main.Memories.Journal;
using Awaken.TG.Main.UI.Components.Tabs;
using Awaken.TG.MVC;

namespace Awaken.TG.Main.Heroes.CharacterSheet.Journal {
    public partial class JournalUI : CharacterSheetTab<VJournalUI>, JournalSubTabs.ISubTabParent<VJournalUI>, ICharacterSheetTabWithSubTabs {
        static JournalSubTabType s_lastTab;
        public JournalSubTabType CurrentType { get; set; } = JournalSubTabType.Bestiary;
        public Tabs<JournalUI, VJournalTabs, JournalSubTabType, IJournalCategoryTab> TabsController { get; set; }
        public JournalSubTabs.ISubTabParent<VJournalUI> SubTabParent => this;
        public CharacterSheetUI CharacterSheetUI => ParentModel;
        VJournalUI VJournalUI => View<VJournalUI>();

        public void HideTabs() {
            SubTabParent.TabsController.BlockNavigation = true;
            VJournalUI.HideTabs();
        }
        
        public void ShowTabs() {
            SubTabParent.TabsController.BlockNavigation = false;
            VJournalUI.ShowTabs();
        }
        
        public void UpdateEntriesCount(int known, int all, bool showAll) {
            VJournalUI.SetEntriesCount(known, all, showAll);
        }
        
        protected override void AfterViewSpawned(VJournalUI view) {
            CharacterSheetUI.SetHeroOnRenderVisible(false);
            
            TryToOpenOnLastUnlockedEntryTab();
            AddElement(new JournalSubTabs());
        }
        
        public bool TryToggleSubTab(CharacterSheetUI ui) {
            ui.Element<JournalUI>().TabsController.SelectTab(s_lastTab ?? JournalSubTabType.Bestiary);
            return true;
        }
        
        void TryToOpenOnLastUnlockedEntryTab() {
            PlayerJournal playerJournal = World.Only<PlayerJournal>();
            var recentEntry = playerJournal.GetLastUnlockedEntry();
            if (recentEntry.IsValid()) {
                CurrentType = recentEntry.tabType;
            }
        }

        protected override void OnDiscard(bool fromDomainDrop) {
            s_lastTab = CurrentType;
        }
    }
}
