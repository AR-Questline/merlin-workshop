using System;
using System.Collections.Generic;
using System.Linq;
using Awaken.TG.Main.Heroes.CharacterSheet.Items.Panel;
using Awaken.TG.Main.Heroes.CharacterSheet.Items.Panel.Tabs;
using Awaken.TG.Main.Heroes.Items;
using Awaken.TG.Main.UI.ButtonSystem;
using Awaken.TG.MVC;
using Awaken.TG.MVC.Events;
using UnityEngine;

namespace Awaken.TG.Main.Heroes.CharacterSheet.Items.Management {
    public abstract class ItemsBatchBySubTabHandler : ItemsBatchHandler {
        Prompt _nextSubTabPrompt;
        int _subTabIndex;
        List<ItemsTabType> _subTabsInOrder;
        
        protected abstract string PromptNameAll { get; }
        
        IEnumerable<Item> Items => ParentModel.Items;

        public override void Initialize(Prompts prompts, ItemsUI itemsUI, Action<Item> batchAction, Transform promptsHost = null) {
            ItemsUI = itemsUI;
            _batchAction = batchAction;
            _batchPrompt = prompts.AddPrompt(Prompt.Hold(KeyBinding, $"{PromptName}:", () => InvokePromptActionAsync().Forget(), Prompt.Position.First), this, promptsHost);
            _nextSubTabPrompt = prompts.AddPrompt(Prompt.Tap(KeyBinding, string.Empty, NextSubTab, Prompt.Position.First), this, promptsHost);
            World.EventSystem.ListenTo(EventSelector.AnySource, ItemsTabs.TabEvents.Events.TabsChanged, this, () => RefreshSubTabs());
            RefreshSubTabs();
            _batchProgressBlend = World.SpawnView<VInProgressBlend>(this);
            _batchProgressBlend.Hide();
        }
        
        void RefreshSubTabs(int initialIndex = -1) {
            _subTabIndex = initialIndex;
            _subTabsInOrder = BuildSubTabsList();
            FilterSubtabs();
            NextSubTab();
        }
        
        List<ItemsTabType> BuildSubTabsList() {
            var subTabsList = ItemsUI.CurrentType != ItemsTabType.Others 
                ? ItemsUI.SubTabsInOrder?.ToList() ?? new List<ItemsTabType>()
                : new List<ItemsTabType>();
            
            subTabsList.Insert(0, ItemsUI.CurrentType);
            return subTabsList;
        }
        
        void FilterSubtabs() {
            _subTabsInOrder?.RemoveAll(tab => !Items.Any(item => ValidateItem(item) && tab.Contains(item)));
        }
        
        void NextSubTab() {
            bool hasSubTabs = _subTabsInOrder is { Count: > 0 };
            _nextSubTabPrompt.SetupState(hasSubTabs, hasSubTabs);
            _batchPrompt.SetupState(hasSubTabs, hasSubTabs);
            if (!hasSubTabs) {
                return;
            }
            
            if (_subTabsInOrder is { Count:1 }) {
                _nextSubTabPrompt.SetupState(false, false);
                _batchPrompt.SetupState(true, true);
                _batchPrompt.ChangeName($"{PromptName}: {_subTabsInOrder.First().Title}");
                _subTabIndex = 0;
                return;
            }
            
            _subTabIndex = (_subTabIndex + 1) % _subTabsInOrder.Count;
            _batchPrompt.ChangeName($"{PromptNameAll}:");
            _nextSubTabPrompt.ChangeName($"{_subTabsInOrder.ElementAt(_subTabIndex).Title}");
        }
        
        protected override void InvokePromptAction() {
            var subTabType = _subTabsInOrder.ElementAtOrDefault(_subTabIndex);
            var currentTabItems = Items.Where(item => ValidateItem(item) && subTabType!.Contains(item)).ToList();
            BatchItems(currentTabItems);
            RefreshSubTabs(_subTabIndex - 1);
            _inProgress = false;
        }
    }
}