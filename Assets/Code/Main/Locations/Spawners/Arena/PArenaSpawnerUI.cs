using System;
using System.Collections.Generic;
using System.Linq;
using Awaken.TG.Main.AudioSystem;
using Awaken.TG.Main.Locations.Setup;
using Awaken.TG.Main.Memories;
using Awaken.TG.Main.Scenes.SceneConstructors;
using Awaken.TG.Main.UIToolkit.CustomControls;
using Awaken.TG.Main.UIToolkit.PresenterData;
using Awaken.TG.MVC;
using UnityEngine.UIElements;

namespace Awaken.TG.Main.Locations.Spawners.Arena {
    public class PArenaSpawnerUI : AdditivePresenter<PArenaSpawnerData, ArenaSpawnerUI> {
        static string s_prefsKeyRecent = $"{nameof(ArenaSpawnerData)};lastSelectedTab";
        
        const string TabClassName = "tab";
        const string CurrentlySelectedTabClassName = "currently-selected-tab";

        const string SearchFieldName = "search-field";
        const string DestroyAllButtonName = "destroy-all-button";

        const string NewTabName = "new";
        const string EncountersTabName = "encounters";
        const string OtherTabName = "templates";

        const string NameSortButtonName = "name-sort-button";
        const string ThreatLevelSortButtonName = "threat-level-sort-button";
        const string FactionSortButtonName = "faction-sort-button";
        const string FavouritesSortButtonName = "favourites-sort-button";
        const string DisplayNoneClassName = "display-none";

        const string ListViewMainName = "list-view-main";
        const string RecentlyChoseListViewName = "recently-chose-list";
        const string FavouritesListViewName = "favourites-list";

        readonly ArenaSpawnerData _data;
        readonly List<IArenaSpawnerEntry> _filteredData;
        readonly Action<LocationTemplate> _spawn;

        TextInputBaseField<string> _searchField;
        string[] _searchStrings = Array.Empty<string>();

        VisualElement _nameSortButton;
        VisualElement _threatLevelSortButton;
        VisualElement _factionSortButton;
        VisualElement _favouritesSortButton;
        List<VisualElement> _sortButtons;

        GenericButton _destroyAllButton;

        ListView _mainListView;
        ListView _recentlyChoseListView;
        ListView _favoritesListView;

        VisualTreeAsset _listElementPrototype;
        
        SortingType _currentSortingType = SortingType.None;
        bool _isSortingAscending = true;

        public string PersistentTabSelected {
            get => PrefMemory.GetString(s_prefsKeyRecent);
            set => PrefMemory.Set($"{s_prefsKeyRecent}", value, false);
        }
        
        enum SortingType {
            None,
            Name,
            ThreatLevel,
            Faction,
            Favourites
        }

        public PArenaSpawnerUI(PArenaSpawnerData data, VisualElement parent, ArenaSpawnerData arenaSpawnerData, Action<LocationTemplate> spawn) : base(data,
            parent) {
            _data = arenaSpawnerData;
            _spawn = spawn;
            _filteredData = new List<IArenaSpawnerEntry>();
        }

        public void Setup(VisualTreeAsset listElementPrototype) {
            _listElementPrototype = listElementPrototype;
            _filteredData.AddRange(_data.News);

            RegisterTabCallbacks();
            InitSearchField();
            InitSorting();
            InitList(_mainListView, _filteredData, true);
            InitList(_recentlyChoseListView, _data.RecentChoices, false);
            InitList(_favoritesListView, _data.Favourites, false);
            UpdateSorting(SortingType.Name);
            SelectTab(PersistentTabSelected);
        }

        protected override void CacheVisualElements(VisualElement contentRoot) {
            _searchField = contentRoot.Query<TextInputBaseField<string>>(SearchFieldName);
            _mainListView = contentRoot.Query<ListView>(ListViewMainName);
            _recentlyChoseListView = contentRoot.Query<ListView>(RecentlyChoseListViewName);
            _favoritesListView = contentRoot.Query<ListView>(FavouritesListViewName);
            _destroyAllButton = contentRoot.Query<GenericButton>(DestroyAllButtonName);
            _nameSortButton = contentRoot.Query<VisualElement>(NameSortButtonName);
            _threatLevelSortButton = contentRoot.Query<VisualElement>(ThreatLevelSortButtonName);
            _factionSortButton = contentRoot.Query<VisualElement>(FactionSortButtonName);
            _favouritesSortButton = contentRoot.Query<VisualElement>(FavouritesSortButtonName);
        }

        protected override void OnFullyInitialized() {
            Content.style.flexGrow = 1;
            InitDestroyAllButton();
        }

        protected override void OnBeforeDiscard() {
            _destroyAllButton.ClickAction -= TargetModel.ParentModel.KillSpawned;
            _nameSortButton.UnregisterCallback<ClickEvent>(OnNameSortButtonClick);
            _threatLevelSortButton.UnregisterCallback<ClickEvent>(OnThreatLevelSortButtonClick);
            _factionSortButton.UnregisterCallback<ClickEvent>(OnFactionSortButtonClick);
            _favouritesSortButton.UnregisterCallback<ClickEvent>(OnFavouritesSortButtonClick);
        }

        void InitSearchField() {
            _searchField.RegisterValueChangedCallback(OnSearchFieldChange);
            _searchField.Focus();
        }

        void InitSorting() {
            _sortButtons = new List<VisualElement> {
                _nameSortButton,
                _threatLevelSortButton,
                _factionSortButton,
                _favouritesSortButton
            };
            _nameSortButton.RegisterCallback<ClickEvent>(OnNameSortButtonClick);
            _threatLevelSortButton.RegisterCallback<ClickEvent>(OnThreatLevelSortButtonClick);
            _factionSortButton.RegisterCallback<ClickEvent>(OnFactionSortButtonClick);
            _favouritesSortButton.RegisterCallback<ClickEvent>(OnFavouritesSortButtonClick);
        }

        void OnNameSortButtonClick(ClickEvent evt) {
            UpdateSorting(SortingType.Name);
            RefreshSorting();
            FMODManager.PlayOneShot(CommonReferences.Get.AudioConfig.ButtonClickedSound);
        }

        void OnThreatLevelSortButtonClick(ClickEvent evt) {
            UpdateSorting(SortingType.ThreatLevel);
            RefreshSorting();
            FMODManager.PlayOneShot(CommonReferences.Get.AudioConfig.ButtonClickedSound);
        }

        void OnFactionSortButtonClick(ClickEvent evt) {
            UpdateSorting(SortingType.Faction);
            RefreshSorting();
            FMODManager.PlayOneShot(CommonReferences.Get.AudioConfig.ButtonClickedSound);
        }

        void OnFavouritesSortButtonClick(ClickEvent evt) {
            UpdateSorting(SortingType.Favourites);
            RefreshSorting();
            FMODManager.PlayOneShot(CommonReferences.Get.AudioConfig.ButtonClickedSound);
        }

        void UpdateSorting(SortingType sortingType) {
            if (sortingType == _currentSortingType) {
                _isSortingAscending = !_isSortingAscending;
            } else {
                _isSortingAscending = true;
            }

            _currentSortingType = sortingType;
        }

        void RefreshSorting() {
            foreach (var button in _sortButtons) {
                ((VisualElement)button.Query<VisualElement>("arrow-down")).AddToClassList(DisplayNoneClassName);
                ((VisualElement)button.Query<VisualElement>("arrow-up")).AddToClassList(DisplayNoneClassName);
            }

            var currentSortButton = _sortButtons[(int)_currentSortingType - 1];
            if (_isSortingAscending) {
                ((VisualElement)currentSortButton.Query<VisualElement>("arrow-down")).RemoveFromClassList(DisplayNoneClassName);
            } else {
                ((VisualElement)currentSortButton.Query<VisualElement>("arrow-up")).RemoveFromClassList(DisplayNoneClassName);
            }

            switch (_currentSortingType) {
                case SortingType.Name:
                    if (_isSortingAscending) {
                        _filteredData.Sort((a, b) => string.Compare(a.Label, b.Label, StringComparison.OrdinalIgnoreCase));
                    } else {
                        _filteredData.Sort((a, b) => string.Compare(b.Label, a.Label, StringComparison.OrdinalIgnoreCase));
                    }

                    break;
                case SortingType.ThreatLevel:
                    if (_isSortingAscending) {
                        _filteredData.Sort((a, b) => a.ThreatLevel.CompareTo(b.ThreatLevel));
                    } else {
                        _filteredData.Sort((a, b) => b.ThreatLevel.CompareTo(a.ThreatLevel));
                    }

                    break;
                case SortingType.Faction:
                    if (_isSortingAscending) {
                        _filteredData.Sort((a, b) => string.Compare(a.FactionName, b.FactionName, StringComparison.OrdinalIgnoreCase));
                    } else {
                        _filteredData.Sort((a, b) => string.Compare(b.FactionName, a.FactionName, StringComparison.OrdinalIgnoreCase));
                    }

                    break;
                case SortingType.Favourites:
                    if (_isSortingAscending) {
                        _filteredData.Sort((a, b) => {
                            var ret = IsFavourite(b).CompareTo(IsFavourite(a));
                            if (ret == 0) ret = string.Compare(a.Label, b.Label, StringComparison.OrdinalIgnoreCase);
                            return ret;
                        });
                    } else {
                        _filteredData.Sort((a, b) => {
                            var ret = IsFavourite(a).CompareTo(IsFavourite(b));
                            if (ret == 0) ret = string.Compare(a.Label, b.Label, StringComparison.OrdinalIgnoreCase);
                            return ret;
                        });
                    }

                    break;
            }

            _mainListView.RefreshItems();
        }

        void OnSearchFieldChange(ChangeEvent<string> evt) {
            UpdateFilteredData();
            _mainListView.RefreshItems();
        }

        void InitDestroyAllButton() {
            _destroyAllButton.ClickAction += TargetModel.ParentModel.KillSpawned;
        }

        void InitList(ListView listView, List<IArenaSpawnerEntry> dataSource, bool fullDetails) {
            listView.Q<ScrollView>().verticalScrollerVisibility = ScrollerVisibility.AlwaysVisible;
            listView.Q<ScrollView>().mouseWheelScrollSize *= 20;
            listView.makeItem = MakeItem;
            listView.bindItem = (element, i) => BindItem(element, i, dataSource, fullDetails);
            listView.unbindItem = UnbindItem;
            listView.itemsSource = dataSource;
        }

        VisualElement MakeItem() {
            PArenaSpawnerListElement entryLogic = new(_listElementPrototype.Instantiate());
            World.BindPresenter(TargetModel, entryLogic);
            entryLogic.Content.userData = entryLogic;
            return entryLogic.Content;
        }

        void BindItem(VisualElement element, int i, List<IArenaSpawnerEntry> dataSource, bool fullDetails) {
            if (element.userData is not PArenaSpawnerListElement entryLogic) {
                return;
            }

            var entry = dataSource[i];
            entryLogic.Init(entry, fullDetails, fullDetails, IsFavourite(entry), () => OnFavouriteClick(entry));
            entryLogic.Clicked += OnArenaSpawnerListElementClick;
        }

        void UnbindItem(VisualElement element, int i) {
            if (element.userData is not PArenaSpawnerListElement entryLogic) {
                return;
            }

            entryLogic.Clicked -= OnArenaSpawnerListElementClick;
            entryLogic.Dispose();
        }

        bool IsFavourite(IArenaSpawnerEntry entry) {
            return _data.Favourites.Contains(entry);
        }

        void OnArenaSpawnerListElementClick(PArenaSpawnerListElement listElement) {
            _data.AddRecentlyChose(listElement.Entry);
            foreach (var locationTemplate in listElement.Entry.LocationTemplates) {
                _spawn.Invoke(locationTemplate);
            }

            _recentlyChoseListView.RefreshItems();
        }

        void OnFavouriteClick(IArenaSpawnerEntry entry) {
            _data.AddOrRemoveFavourite(entry);
            _mainListView.RefreshItems();
            _favoritesListView.RefreshItems();
            _recentlyChoseListView.RefreshItems();
        }

        void RegisterTabCallbacks() {
            UQueryBuilder<Label> tabs = GetAllTabs();
            tabs.ForEach(tab => tab.RegisterCallback<ClickEvent>(TabOnClick));
        }

        void TabOnClick(ClickEvent evt) {
            FMODManager.PlayOneShot(CommonReferences.Get.AudioConfig.ButtonClickedSound);
            Label clickedTab = evt.currentTarget as Label;
            if (!TabIsCurrentlySelected(clickedTab)) {
                PersistentTabSelected = clickedTab != null ? clickedTab.name : string.Empty;
                GetAllTabs().Where(tab => tab != clickedTab && TabIsCurrentlySelected(tab)).ForEach(UnselectTab);
                SelectTab(clickedTab);
            }
        }

        bool TabIsCurrentlySelected(Label tab) {
            return tab.ClassListContains(CurrentlySelectedTabClassName);
        }
        
        void SelectTab(Label tab) {
            tab.AddToClassList(CurrentlySelectedTabClassName);
            UpdateFilteredData();
            RefreshSorting();
            _mainListView.Rebuild();
        }

        void SelectTab(string tab) {
            var tabToSelect = GetAllTabs().Where(t => t.name == tab).First() ?? GetAllTabs().First();
            SelectTab(tabToSelect);
        }

        void UnselectTab(Label tab) {
            tab.RemoveFromClassList(CurrentlySelectedTabClassName);
        }

        void UpdateFilteredData() {
            string searchText = _searchField.text;
            var currentlySelectedTab = GetCurrentlySelectedTab().name;
            
            _filteredData.Clear();
            _searchStrings = string.IsNullOrWhiteSpace(searchText)
                ? Array.Empty<string>()
                : searchText.Split(' ').Where(s => !string.IsNullOrWhiteSpace(s)).ToArray();
            
            switch (currentlySelectedTab) {
                case NewTabName:
                    _filteredData.AddRange(GetFilteredData(_data.News));
                    break;
                case EncountersTabName:
                    _filteredData.AddRange(GetFilteredData(_data.Encounters));
                    break;
                case OtherTabName:
                    _filteredData.AddRange(GetFilteredData(_data.Other));
                    break;
            }
            return;

            IEnumerable<IArenaSpawnerEntry> GetFilteredData(List<IArenaSpawnerEntry> data) {
                return string.IsNullOrWhiteSpace(searchText) ? 
                    data.OrderBy(e => e.Label) : 
                    data.Where(MatchesSearch).OrderByDescending(MatchScore);
            }
            
            bool MatchesSearch(IArenaSpawnerEntry entry) => _searchStrings.All(s => entry.Label.Contains(s, StringComparison.InvariantCultureIgnoreCase));
            int MatchScore(IArenaSpawnerEntry entry) => 0;
        }
        
        /// Helpers
        Label GetCurrentlySelectedTab() {
            return Content.Q<Label>(className: CurrentlySelectedTabClassName);
        }

        UQueryBuilder<Label> GetAllTabs() {
            return Content.Query<Label>(className: TabClassName);
        }
    }
}