using Awaken.TG.Main.Heroes.CharacterSheet.Items.Panel.Tabs;
using Awaken.TG.Main.Localization;
using Awaken.TG.Main.UI.ButtonSystem;
using Awaken.TG.Main.Utility;
using Awaken.TG.MVC;
using Awaken.TG.Utility;
using System.Collections.Generic;
using System.Linq;
using Awaken.Utility;
using Awaken.Utility.Collections;
using UnityEngine;

namespace Awaken.TG.Main.Heroes.CharacterSheet.Items.Panel.List {
    public class VCItemSorting : ViewComponent<ItemsListUI>, IPromptHost {
        [SerializeField] VGenericPromptUI filterPrompt, sortPrompt;
        
        public Transform PromptsHost => transform;
        
        List<ItemsSorting> _sortings;
        int _currentSorting;
        Prompt _sortPrompt;
        bool _filtersAvailable;
        ItemsTabType[] _filters;
        int _currentFilter;
        Prompt _filterPrompt;

        protected override void OnAttach() {
            var prompts = Target.AddElement(new Prompts(this));
            _sortPrompt = Prompt.Tap(KeyBindings.UI.Items.SortItems, LocTerms.UIItemsChangeSorting.Translate(), NextSorting).AddAudio();
            _filterPrompt = Prompt.Tap(KeyBindings.UI.Items.FilterItems, LocTerms.UIItemsChangeFilter.Translate(), NextFilter).AddAudio();
            
            prompts.BindPrompt(_sortPrompt, Target, sortPrompt);
            prompts.BindPrompt(_filterPrompt, Target, filterPrompt);

            SetInitialFilter(Target.TabType);
            SetInitialSorting(Target.ItemsUI.Config.SortingTab ?? Target.TabType);
        }

        public void NextSorting() {
            _currentSorting = (_currentSorting + 1) % _sortings.Count;
            var sorting = _sortings[_currentSorting];
            RefreshSortingPrompt(sorting);
            Target.Sort(sorting);
        }

        public void NextFilter() {
            if (!_filtersAvailable) {
                return;
            }
            
            _currentFilter = (_currentFilter + 1) % _filters.Length;
            var filterTabType = _filters[_currentFilter];
            
            RefreshPromptName(_filterPrompt, $"{LocTerms.UIItemsChangeFilter.Translate().ColoredText(ARColor.MainGrey).FontLight()} {filterTabType.Title.Italic().ColoredText(ARColor.MainWhite).FontSemiBold()}");
            Target.Filter(filterTabType);
        }
        
        public void ChangeSortingPromptState(bool active, bool visible) {
            _sortPrompt.SetupState(visible, active);
        }
        
        public void ChangeFilterPromptState(bool active, bool visible) {
            _filterPrompt.SetupState(visible, active);
        }
        
        void SetInitialSorting(ItemsTabType tabType) {
            _sortings = ItemsSorting.GetItemsSorting(tabType);
            _currentSorting = _sortings.IndexOf(Target.ItemsUI.GetCurrentSorting(tabType));
            if (_currentSorting < 0) {
                _currentSorting = 0;
                Target.OverrideSorting(_sortings[_currentSorting]);
            }
            
            RefreshSortingPrompt(_sortings[_currentSorting]);
        }

        void SetInitialFilter(ItemsTabType tabType) {
            var config = Target.ItemsUI.Config;
            _filters = config.EquipmentSlotType?.FilterTabs ?? tabType.SubTabs;
            _filtersAvailable = !config.UseCategoryList && config.UseFilter && (_filters?.Any() ?? false);
            _filterPrompt.SetupState(_filtersAvailable, _filtersAvailable);
            
            if (!_filtersAvailable) {
                return;
            }

            ItemsTabType filterTabType = Target.ItemsUI.CurrentFiltersMap.ContainsKey(tabType) ? tabType : ItemsTabType.All;
            _currentFilter = _filters.IndexOf(filterTabType);
            if (_currentFilter < 0) {
                _currentFilter = 0;
                Target.OverrideFilter(_filters[_currentFilter]);
            }
            
            RefreshPromptName(_filterPrompt, $"{LocTerms.UIItemsChangeFilter.Translate().ColoredText(ARColor.MainGrey).FontLight()} {_filters[_currentFilter].Title.Italic().ColoredText(ARColor.MainWhite).FontSemiBold()}");
        }

        void RefreshSortingPrompt(ItemsSorting sorting) {
            RefreshPromptName(_sortPrompt, $"{LocTerms.UIItemsChangeSorting.Translate().ColoredText(ARColor.MainGrey).FontLight()} {sorting.Name.Italic().ColoredText(ARColor.MainWhite).FontSemiBold()}");
        }

        void RefreshPromptName(Prompt prompt, string name) {
            prompt.ChangeName(name);
        }
    }
}