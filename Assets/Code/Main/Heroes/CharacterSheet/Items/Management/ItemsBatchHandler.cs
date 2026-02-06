using System;
using System.Collections.Generic;
using System.Linq;
using Awaken.TG.Main.Fights.Utils;
using Awaken.TG.Main.Heroes.CharacterSheet.Items.Panel;
using Awaken.TG.Main.Heroes.Items;
using Awaken.TG.Main.UI.ButtonSystem;
using Awaken.TG.Main.Utility;
using Awaken.TG.MVC;
using Awaken.TG.MVC.Elements;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace Awaken.TG.Main.Heroes.CharacterSheet.Items.Management {
    public abstract partial class ItemsBatchHandler : Element<IItemsUIConfig> {
        protected Prompt _batchPrompt;
        protected Action<Item> _batchAction;
        protected bool _inProgress;
        protected VInProgressBlend _batchProgressBlend;
        
        public sealed override bool IsNotSaved => true;
        
        protected abstract string PromptName { get; }
        protected abstract KeyBindings KeyBinding { get; }
        protected ItemsUI ItemsUI { get; set; }
        IEnumerable<Item> Items => ParentModel.Items;

        public virtual void Initialize(Prompts prompts, ItemsUI itemsUI, Action<Item> batchAction, Transform promptsHost = null) {
            ItemsUI = itemsUI;
            _batchAction = batchAction;
            _batchPrompt = prompts.AddPrompt(Prompt.Hold(KeyBinding, PromptName, () => InvokePromptActionAsync().Forget(), Prompt.Position.First), this);
            ItemsUI.ListenTo(ItemsUI.Events.ItemsCollectionChanged, RefreshPromptState, this);
            RefreshPromptState();
            _batchProgressBlend = World.SpawnView<VInProgressBlend>(this);
            _batchProgressBlend.Hide();
        }
        
        protected abstract bool ValidateItem(Item item);
        
        protected async UniTaskVoid InvokePromptActionAsync() {
            _inProgress = true;
            
            _batchProgressBlend.Show();
            if (!await AsyncUtil.DelayFrame(this, 2)) {
                return;
            }
            
            InvokePromptAction();
            
            if (!await AsyncUtil.WaitUntil(this, () => !_inProgress)) {
                return;
            }
            
            _batchProgressBlend.Hide();
        }
        
        protected virtual void InvokePromptAction() {
            var batchedItems = Items.Where(ValidateItem).ToList();
            BatchItems(batchedItems);
            RefreshPromptState();
            _inProgress = false;
        }
        
        protected void BatchItems(List<Item> items) {
            foreach (var item in items) {
                _batchAction(item);
                ItemsUI.GetItemsListElementWithItem(item)?.Discard();
            }
            ItemsUI.Trigger(ItemsUI.Events.ItemsCollectionChanged, Items);
            ItemsUI.FullRefresh();
        }
        
        void RefreshPromptState() {
            bool hasValidItems = Items.Any(ValidateItem);
            _batchPrompt.SetupState(hasValidItems, hasValidItems);
        }
    }
}