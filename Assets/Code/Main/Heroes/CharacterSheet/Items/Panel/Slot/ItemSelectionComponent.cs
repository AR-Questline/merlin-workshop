using System;
using Awaken.TG.Main.Heroes.Items;
using Awaken.TG.Main.Heroes.Items.Tooltips;
using Awaken.TG.Main.Heroes.Items.Tooltips.Descriptors;
using Awaken.TG.Main.Utility.Semaphores;
using Awaken.TG.MVC;
using Awaken.TG.MVC.UI.Handlers.Selections;
using JetBrains.Annotations;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Awaken.TG.Main.Heroes.CharacterSheet.Items.Panel.Slot {
    public class ItemSelectionComponent : ItemSlotComponent, ISemaphoreObserver {
        [SerializeField] bool separateHoverAndSelectGraphic;
        [SerializeField, ShowIf(nameof(separateHoverAndSelectGraphic)), CanBeNull] GameObject selectGraphicGroup;
        [SerializeField] bool blockTooltipRefreshOnHover;
        [SerializeField] bool blockTooltipRefreshOnClick;
        [SerializeField] TooltipPosition tooltipPositionLeft;
        [SerializeField] TooltipPosition tooltipPositionRight;
        
        Item _item;
        IItemDescriptor _itemDescriptor;
        
        CoyoteSemaphore _isHovered;

        public event Action OnHoverStarted;
        public event Action OnHoverEnded;
        public event Action OnSelect;
        public event Action OnDeselect;

        protected override bool MiddleVisibilityOf((Item, View, ItemDescriptorType) data) => true;

        protected override void Refresh(Item item, View view, ItemDescriptorType itemDescriptorType) {
            if (_item == item) return;
            _item = item;
            
            if (item == null) {
                _itemDescriptor = null;
            } else {
                _itemDescriptor = itemDescriptorType?.GetItemDescriptor(_item) ?? new ExistingItemDescriptor(_item);
            }
        }

        public void ForceRefresh(Item item) {
            Refresh(item, null, null);
            Unhover();
            Deselect();
            Hover();
            Select();
        }

        public void ForceHover(Item item) {
            Refresh(item, null, null);
            Hover();
        }
        
        public void ForceUnhover() {
            Unhover();
        }
        
        public void ForceUnselect() {
            World.Only<Selection>().Select(null);

            if (separateHoverAndSelectGraphic && selectGraphicGroup != null) {
                selectGraphicGroup.SetActive(false);
            } else {
                SetInternalVisibility(false);
            }
            
            if (!blockTooltipRefreshOnClick) {
                SetupTooltip();
            }
        }

        void Start() {
            _isHovered = new CoyoteSemaphore(this);
            SetInternalVisibility(false);
            if (selectGraphicGroup != null) {
                selectGraphicGroup.SetActive(false);
            }
        }

        void Update() {
            _isHovered.Update();
        }

        void OnDisable() {
            _isHovered.ForceDown();
        }

        public void Select() {
            HandleSelection(true);
        }
        
        public void Deselect() {
            HandleSelection(false);
        }

        public void NotifyHovered() {
            _isHovered.Notify();
        }

        public void ResetHoveredState() {
            _isHovered = new CoyoteSemaphore(this);
        }

        void HandleSelection(bool selectState) {
            if (selectState) {
                World.Only<Selection>().Select(_item);
            } else {
                World.Only<Selection>().Deselect(_item);
            }
            
            if (separateHoverAndSelectGraphic && selectGraphicGroup != null) {
                selectGraphicGroup.SetActive(selectState);
            } else {
                SetInternalVisibility(selectState);
            }

            if (selectState) {
                OnSelect?.Invoke();
            } else {
                OnDeselect?.Invoke();
            }

            if (!blockTooltipRefreshOnClick) {
                SetupTooltip();
            }
        }

        void SetupTooltip() {
            var tooltip = World.Any<ItemTooltipUI>();
            if (tooltip != null) {
                if (_item == null || _item.HasBeenDiscarded) {
                    _itemDescriptor = null;
                }
                
                tooltip.SetDescriptor(_itemDescriptor);
                tooltip.SetPosition(tooltipPositionLeft, tooltipPositionRight);
            }
        }

        void Hover() {
            World.Only<Selection>().Select(_item);
            SetInternalVisibility(true);
            OnHoverStarted?.Invoke();

            if (!blockTooltipRefreshOnHover) {
                SetupTooltip();
            }
        }
        
        void Unhover() {
            World.Only<Selection>().Deselect(_item);
            SetInternalVisibility(false);
            OnHoverEnded?.Invoke();
            
            if (!blockTooltipRefreshOnHover) {
                World.Any<ItemTooltipUI>()?.ResetDescriptor(_itemDescriptor);
            }
        }

        void ISemaphoreObserver.OnUp() => Hover();
        void ISemaphoreObserver.OnDown() => Unhover();
    }
}