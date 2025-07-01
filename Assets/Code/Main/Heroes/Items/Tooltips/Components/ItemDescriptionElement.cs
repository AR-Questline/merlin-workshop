using System;
using Awaken.TG.Main.Heroes.CharacterSheet.Items.Panel.Slot;
using Awaken.TG.MVC;
using Sirenix.OdinInspector;
using TMPro;
using UnityEngine;

namespace Awaken.TG.Main.Heroes.Items.Tooltips.Components {
    [Serializable]
    public class ItemDescriptionElement : ViewComponent {
        [Title("Labels")]
        [SerializeField] TextMeshProUGUI titleLabel;
        [SerializeField] TextMeshProUGUI descriptionLabel;
        [Title("Icons")]
        [SerializeField] ItemSlotUI itemSlot;
        [SerializeField] GameObject genericIcon;
        [SerializeField] GameObject itemSlotParent;
        
        Item _currentItem;

        public void Setup(Transform parent, string description, ItemSlotUI.VisibilityConfig visibilityConfig, string title = null) {
            Setup(parent, description, title, true, visibilityConfig);
        }
        
        public void Setup(Transform parent, string description, string title = null) {
            Setup(parent, description, title, false);
        }
        
        public Item AddItemIcon(ItemTemplate template, View view) {
            _currentItem = new Item(template);
            _currentItem.MarkedNotSaved = true;
            
            World.Add(_currentItem);
            itemSlot.Setup(_currentItem, view);
            
            return _currentItem;
        }
        
        public void OnReleaseElement() {
            gameObject.SetActive(false);
        }
        
        public void OnDestroyElement() {
            Destroy(gameObject);
        }
        
        void Setup(Transform parent, string description, string title, bool useItemIcon, ItemSlotUI.VisibilityConfig visibilityConfig = default) {
            transform.SetParent(parent);
            transform.SetAsLastSibling();
            descriptionLabel.text = description;

            if (CanShowIcon()) {
                genericIcon.SetActive(!useItemIcon);
                itemSlotParent.SetActive(useItemIcon);

                if (useItemIcon) {
                    itemSlot.SetVisibilityConfig(visibilityConfig);
                }
            }

            titleLabel?.gameObject.SetActive(!string.IsNullOrEmpty(title));

            if (titleLabel != null && !string.IsNullOrEmpty(title)) {
                titleLabel.text = title;
            }
            
            _currentItem?.Discard();
        }
        
        bool CanShowIcon() {
            return itemSlot != null && genericIcon != null;
        }
    }
}