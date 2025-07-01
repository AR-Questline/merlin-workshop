using System;
using System.Collections.Generic;
using System.Linq;
using Awaken.TG.Main.Heroes.CharacterSheet.Journal.Content;
using Awaken.TG.Main.Heroes.CharacterSheet.Journal.Entries;
using Awaken.TG.Main.Heroes.Fishing;
using Awaken.TG.Main.Localization;
using Awaken.TG.Main.Memories.Journal;
using Awaken.TG.Main.Memories.Journal.Entries;
using Awaken.TG.Main.Memories.Journal.Entries.Implementations;
using Awaken.TG.Main.Scenes.SceneConstructors;
using Awaken.TG.Main.Tutorials;
using Awaken.TG.Main.UI.Popup.PopupContents;
using Awaken.TG.MVC;
using Awaken.TG.MVC.Events;
using Awaken.TG.Utility;

namespace Awaken.TG.Main.Heroes.CharacterSheet.Journal.Tabs {
    public abstract partial class JournalCategoryUI<TUIData, TData, TView> : JournalCategoryTab<TView>, IJournalCategoryUI where TUIData : IJournalEntryData where TView : View, IVJournalCategoryUI {
        protected readonly List<TData> allEntries = new();
        readonly List<TUIData> _knownEntries = new();
        readonly List<JournalCategoryDropdownUI> _categoriesDropdown = new();

        protected virtual bool ShowAllCounter => false;
        protected abstract IEnumerable<TData> GatherAllEntries();
        protected abstract IEnumerable<(string name, IEnumerable<TUIData> entries)> GatherCategories();
        protected TView View => View<TView>();

        protected override void AfterViewSpawned(TView view) {
            base.AfterViewSpawned(view);
            PopulateEntries();
        }

        public void SelectEntry(IJournalEntryData entryData) {
            this.Trigger(IJournalCategoryUI.Events.EntrySelected, entryData);
        }

        protected void PopulateEntries() {
            allEntries.AddRange(GatherAllEntries());
            
            foreach (var categories in GatherCategories()) {
                var category = AddElement(new JournalCategoryDropdownUI(categories.name));
                _categoriesDropdown.Add(category);

                foreach (var entry in categories.entries) {
                    _knownEntries.Add(entry);
                    category.AddElement(new JournalButtonEntryUI(entry));
                }
            }
            
            View.EntriesParent.gameObject.SetActive(_knownEntries.Count > 0);
            View.ShowNoEntriesInfo(_knownEntries.Count == 0);
            
            if (_knownEntries.Count > 0) {
                if (_categoriesDropdown.Count == 1) {
                    _categoriesDropdown[0].ToggleCategoryAsync().Forget();
                }

                var requestedEntry = !string.IsNullOrEmpty(ParentModel.RequestedEntryName) ? _knownEntries.FirstOrDefault(e => e.Name == ParentModel.RequestedEntryName) : default;
                var entry = string.IsNullOrEmpty(requestedEntry?.Name) ? _knownEntries[0] : requestedEntry;
                ParentModel.RequestedEntryName = null;
                SelectEntry(entry);
            }
            
            ParentModel.UpdateEntriesCount(_knownEntries.Count, allEntries.Count, ShowAllCounter);
        }
        
        protected void ClearEntries() {
            RemoveElementsOfType<JournalCategoryDropdownUI>();
            RemoveElementsOfType<JournalButtonEntryUI>();
            allEntries.Clear();
            _knownEntries.Clear();
            _categoriesDropdown.Clear();
        }
        
        protected static string[] GetUnlockedSubEntries(EntryData data) {
            return data.GetEntries().Where(e => e.Condition.IsMet()).Select(e => e.TextToShow.Translate()).ToArray();
        }
    }
    
    public interface IJournalCategoryUI : IModel {
        public static class Events {
            public static readonly Event<IJournalCategoryUI, IJournalEntryData> EntrySelected = new(nameof(EntrySelected));
        }

        public void SelectEntry(IJournalEntryData entryData);
    }

    public partial class JournalBestiary : JournalCategoryUI<JournalEntryData, BeastiaryRuntime.BeastiaryData, VJournalCategoryUI> {
        protected override bool ShowAllCounter => true;

        protected override IEnumerable<BeastiaryRuntime.BeastiaryData> GatherAllEntries() {
            return World.Only<PlayerJournal>().GetEntries<BeastiaryRuntime.BeastiaryData>();
        }
        
        protected override IEnumerable<(string name, IEnumerable<JournalEntryData> entries)> GatherCategories() {
            var knownEnemies = allEntries.Where(x => x.conditionForEntry.IsMet());
            yield return (LocTerms.JournalTabBestiary.Translate(), GatherKnownEntries(knownEnemies));
        }

        IEnumerable<JournalEntryData> GatherKnownEntries(IEnumerable<BeastiaryRuntime.BeastiaryData> knownEntries) {
            return knownEntries.Select(data => new JournalEntryData(data.EntryName, data.description, GetUnlockedSubEntries(data), data.image));
        }
    }

    public partial class JournalCharacters : JournalCategoryUI<JournalEntryData, CharacterRuntime.CharacterData, VJournalCategoryUI> {
        protected override bool ShowAllCounter => true;

        protected override IEnumerable<CharacterRuntime.CharacterData> GatherAllEntries() {
            return World.Only<PlayerJournal>().GetEntries<CharacterRuntime.CharacterData>();
        }
        
        protected override IEnumerable<(string name, IEnumerable<JournalEntryData> entries)> GatherCategories() {
            var knownCharacters = allEntries.Where(x => x.conditionForEntry.IsMet());
            yield return (LocTerms.JournalTabCharacters.Translate(), GatherKnownEntries(knownCharacters));
        }

        IEnumerable<JournalEntryData> GatherKnownEntries(IEnumerable<CharacterRuntime.CharacterData> knownEntries) {
            return knownEntries.Select(data => new JournalEntryData(data.EntryName, data.description, GetUnlockedSubEntries(data), data.image));
        }
    }

    public partial class JournalLore : JournalCategoryUI<JournalEntryData, LoreEntryRuntime.LoreJournalData, VJournalCategoryUI> {
        protected override bool ShowAllCounter => true;

        protected override IEnumerable<LoreEntryRuntime.LoreJournalData> GatherAllEntries() {
            return World.Only<PlayerJournal>().GetEntries<LoreEntryRuntime.LoreJournalData>();
        }
        
        protected override IEnumerable<(string name, IEnumerable<JournalEntryData> entries)> GatherCategories() {
            var knownPlaces = allEntries.Where(x => x.conditionForEntry.IsMet());
            yield return (LocTerms.JournalTabLore.Translate(), GatherKnownEntries(knownPlaces));
        }

        IEnumerable<JournalEntryData> GatherKnownEntries(IEnumerable<LoreEntryRuntime.LoreJournalData> knownEntries) {
            return knownEntries.Select(data => new JournalEntryData(data.EntryName, data.description, GetUnlockedSubEntries(data), data.image));
        }
    }

    public partial class JournalFish : JournalCategoryUI<JournalEntryData, FishEntry, VJournalCategoryUI> {
        protected override IEnumerable<FishEntry> GatherAllEntries() {
            return Hero.Current.Element<HeroCaughtFish>().caughtFish;
        }

        protected override IEnumerable<(string name, IEnumerable<JournalEntryData> entries)> GatherCategories() {
            yield return (LocTerms.All.Translate(), GatherKnownEntries());
        }

        IEnumerable<JournalEntryData> GatherKnownEntries() {
            foreach (var fish in Hero.Current.Element<HeroCaughtFish>().caughtFish) {
                yield return new JournalEntryData(fish.Name, fish.DescriptionToDisplay(), Array.Empty<string>(), fish.Graphic);
            }
        }
    }

    public partial class JournalTutorials : JournalCategoryUI<JournalEntryData, ITutorialDataOwner, VJournalCategoryUI> {
        protected override IEnumerable<ITutorialDataOwner> GatherAllEntries() {
            var tutorialConfig = CommonReferences.Get.TutorialConfig;
            var graphicTutorials = tutorialConfig.AllGraphicTutorials().ToArray();
            var videoTutorials = tutorialConfig.AllVideoTutorials().ToArray();
            
            foreach (SequenceKey sequenceKey in Enum.GetValues(typeof(SequenceKey))) {
                if (TutorialKeys.IsConsumed(sequenceKey)) {
                    foreach (var graphic in graphicTutorials) {
                        if (graphic.sequenceKey == sequenceKey) {
                            yield return graphic;
                        }
                    }
                    
                    foreach (var video in videoTutorials) {
                        if (video.sequenceKey == sequenceKey) {
                            yield return video;
                        }
                    }
                }
            }
        }
        
        protected override IEnumerable<(string name, IEnumerable<JournalEntryData> entries)> GatherCategories() {
            yield return (LocTerms.JournalTabTutorials.Translate(), GatherKnownEntries());
        }

        IEnumerable<JournalEntryData> GatherKnownEntries() {
            foreach (var tutorial in allEntries) {
                switch (tutorial) {
                    case TutorialConfig.TextTutorial textTutorial: {
                        yield return new JournalEntryData(textTutorial.title, textTutorial.text, Array.Empty<string>());
                        break;
                    }

                    case TutorialConfig.GraphicTutorial graphicTutorial: {
                        var tutorialContent = AddElement(new JournalTutorialContent(graphicTutorial));
                        var content = new DynamicContent(tutorialContent, typeof(VJournalTutorialGraphicContent));
                        yield return new JournalEntryData(graphicTutorial.title, string.Empty, Array.Empty<string>(), graphicTutorial.icon, content);
                        break;
                    }

                    case TutorialConfig.VideoTutorial videoTutorial: {
                        var tutorialContent = AddElement(new JournalTutorialContent(videoTutorial));
                        var content = new DynamicContent(tutorialContent, typeof(VJournalTutorialVideoContent));
                        yield return new JournalEntryData(videoTutorial.title, string.Empty, Array.Empty<string>(), videoTutorial.icon, content);
                        break;
                    }
                }
            }
        }
    }
}
