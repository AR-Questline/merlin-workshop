using Awaken.TG.Main.Heroes.CharacterSheet.Journal.JournalRecipe;
using Awaken.TG.Main.Heroes.CharacterSheet.Journal.Tabs;
using Awaken.TG.Main.Heroes.CharacterSheet.Tabs;
using Awaken.TG.Main.UI.Components.Tabs;

namespace Awaken.TG.Main.Heroes.CharacterSheet.Journal {
    public partial class JournalUI : CharacterSheetTab<VJournalUI>, JournalSubTabs.ISubTabParent<VJournalUI>, ICharacterSheetTabWithSubTabs {
        static JournalSubTabType s_lastTab;
        public JournalSubTabType CurrentType { get; set; } = JournalSubTabType.Bestiary;
        public Tabs<JournalUI, VJournalTabs, JournalSubTabType, IJournalCategoryTab> TabsController { get; set; }
        public JournalSubTabs.ISubTabParent<VJournalUI> SubTabParent => this;
        public string RequestedEntryName { get; set; }
        CharacterSheetUI CharacterSheetUI => ParentModel;
        VJournalUI VJournalUI => View<VJournalUI>();

        public void BackToMainTab() {
            if (TryGetElement(out JournalRecipeUI journalRecipeUI)) {
                journalRecipeUI.Element<JournalRecipeSubTabs>().SetNone();
            }
        }

        public void HideTabs() {
            SubTabParent.TabsController.BlockNavigation = true;
            VJournalUI.HideTabs();
        }
        
        public void ShowTabs() {
            SubTabParent.TabsController.BlockNavigation = false;
            VJournalUI.ShowTabs();
        }
        
        public void SetCountActive(bool active) {
            VJournalUI.SetCountActive(active);
        }
        
        public void UpdateEntriesCount(int known, int all, bool showAll) {
            VJournalUI.SetEntriesCount(known, all, showAll);
        }
        
        protected override void AfterViewSpawned(VJournalUI view) {
            CharacterSheetUI.SetHeroOnRenderVisible(false);
            CharacterSheetUI.AfterViewSpawnedCallback?.Invoke();
            AddElement(new JournalSubTabs());
        }
        
        public bool TryToggleSubTab(CharacterSheetUI ui) {
            ui.Element<JournalUI>().TabsController.SelectTab(s_lastTab ?? JournalSubTabType.Bestiary);
            return true;
        }

        public void OverrideTabAndEntry(string entryName, JournalSubTabType tabType) {
            RequestedEntryName = entryName;
            CurrentType = tabType;
        }

        protected override void OnDiscard(bool fromDomainDrop) {
            s_lastTab = CurrentType;
        }
    }
}
