using System;
using System.Linq;
using Awaken.TG.Main.Crafting.Recipes;
using Awaken.TG.Main.Heroes.CharacterSheet.Items.Panel.Slot;
using Awaken.TG.Main.Heroes.Items.Tooltips.Descriptors;
using Awaken.TG.MVC;
using TMPro;
using UnityEngine;

namespace Awaken.TG.Main.Heroes.Items.Tooltips.Components {
    public interface IPaginatedComponent {
        bool HasPagination { get; }
        bool NextPage();
    }
    
    [Serializable]
    public class ItemTooltipDescriptionsRecipesComponent : ItemTooltipDescriptionsBaseComponent<IRecipe>, IPaginatedComponent {
        [SerializeField] int itemsPerPage = 3;
        [SerializeField] TMP_Text pageInfoText;
        
        public bool HasPagination => _totalPages > 1;
        string GetPaginationInfo() => $"{_currentPage + 1}/{_totalPages}";
        
        int _currentPage;
        int _totalPages;
        
        public override void ToggleSectionActive(bool active) {
            SetParentSectionVisibility(active);
        }

        protected override void Setup(IItemDescriptor descriptor, View view) {
            _totalPages = 0;
            _currentPage = 0;
            
            bool hasContent = descriptor.Read != null && descriptor.Read.Recipes.Any();
            if (descriptor.Read != null) {
                _allElements = descriptor.Read.Recipes.ToArray();
                _totalPages = Mathf.CeilToInt((float)_allElements.Length / itemsPerPage);

                if (_allElements.Length > itemsPerPage) {
                    PrepareDescription(view);
                } else {
                    base.PrepareDescription(view);
                }
                
                UpdatePageInfoText();
            }

            UseReadMore = HasPagination;
            Visibility.SetInternal(hasContent);
        }
        
        protected override void PrepareItemDescription(IRecipe item, ItemDescriptionElement descriptionElement, View view) {
            if (item == null || item.Outcome == null) return;
            
            Item outcomeItem = descriptionElement.AddItemIcon(item.Outcome, view);
            ExistingItemDescriptor descriptor = new (outcomeItem);

            descriptionElement.Setup(ParentSection, descriptor.ItemDescription, ItemSlotUI.VisibilityConfig.OnlyIcon, outcomeItem.DisplayName);        
        }
        
        protected override void PrepareDescription(View view) {
            for (int index = 0; index < itemsPerPage; index++) {
                IRecipe item = _allElements[index];
                PrepareItemDescription(item, _elementPool.Get(), view);
            }
        }
        
        public bool NextPage() {
            _currentPage = (_currentPage + 1) % _totalPages;
            RefreshCurrentPage();
            return true;
        }
        
        void RefreshCurrentPage() {
            foreach (var element in _visibleElements) {
                _elementPool.Release(element);
            }
            _visibleElements.Clear();
            
            var startIndex = _currentPage * itemsPerPage;
            var endIndex = Mathf.Min(startIndex + itemsPerPage, _allElements.Length);
            
            for (int i = startIndex; i < endIndex; i++) {
                PrepareItemDescription(_allElements[i], _elementPool.Get(), TargetView);
            }
            
            SetupElementVisibility();
            UpdatePageInfoText();
        }
        
        void UpdatePageInfoText() {
            if (pageInfoText != null) {
                pageInfoText.SetText(GetPaginationInfo());
            }
        }
    }
}