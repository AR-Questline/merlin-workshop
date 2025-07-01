using System;
using Awaken.TG.Assets;
using Awaken.TG.Main.Character;
using Awaken.TG.Main.Heroes.Items;
using Awaken.TG.MVC;
using Awaken.Utility.GameObjects;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Awaken.TG.Main.Heroes.HUD {
    public class VCSelectedQuickSlot : ViewComponent<Hero> {
        [SerializeField] Image itemIcon;
        [SerializeField] Image[] nextItemIcons = Array.Empty<Image>();
        [SerializeField] TextMeshProUGUI quantityText;
        [SerializeField] GameObject quantityParent;
        [SerializeField] GameObject useStaticPrompt;
        [SerializeField] GameObject nextStaticPrompt;
        [SerializeField] GameObject promptsParent;

        SpriteReference _iconReference;
        SpriteReference[] _nextIconsReference;
        Item[] _nextItems = new Item[EquipmentSlotType.QuickSlots.Length - 1];

        protected override void OnAttach() {
            _nextIconsReference = new SpriteReference[nextItemIcons.Length];
            Target.ListenTo(HeroItems.Events.QuickSlotSelected, UpdateIcon, this);
            Target.ListenTo(HeroItems.Events.QuickSlotUsed, UpdateIcon, this);
            Target.ListenTo(HeroItems.Events.QuickSlotItemUsedWithDelay, UpdateIcon, this);
            
            Target.AfterFullyInitialized(() => {
                Target.HeroItems.ListenTo(ICharacterInventory.Events.PickedUpItem, UpdateIcon, this);
                UpdateIcon();
            });
        }

        public void UpdateIcon() {
            var heroItems = Target.HeroItems;
            bool showItem = heroItems.TryGetSelectedQuickSlotItem(out Item item) &&
                            item is { Icon: { IsSet: true }, Quantity: > 0 };
            heroItems.TryGetAllNextQuickSlotItems(ref _nextItems);

            if (showItem) {
                var newMainItemRef = item.Icon.Get();
                if (_iconReference != newMainItemRef) {
                    _iconReference?.Release();
                    _iconReference = newMainItemRef;
                    _iconReference.SetSprite(itemIcon);
                    quantityText.text = item.Quantity.ToString();
                }
            } else {
                _iconReference?.Release();
                _iconReference = null;
            }

            bool nextSlotAvailable = false;
            for (int i = 0; i < nextItemIcons.Length; i++) {
                bool showNextItem = _nextItems.Length > i && _nextItems[i] is { Icon: { IsSet: true }, Quantity: > 0 };
                nextItemIcons[i].TrySetActiveOptimized(showNextItem);
                if (showNextItem) {
                    nextSlotAvailable = true;
                    var nextItemRef = _nextItems[i].Icon.Get();
                    if (_nextIconsReference[i] != nextItemRef) {
                        _nextIconsReference[i]?.Release();
                        _nextIconsReference[i] = nextItemRef;
                        _nextIconsReference[i].SetSprite(nextItemIcons[i]);
                    }
                } else {
                    _nextIconsReference[i]?.Release();
                    _nextIconsReference[i] = null;
                }
            }
            
            SetVisibility(showItem);
            nextStaticPrompt.SetActiveOptimized(nextSlotAvailable);
            promptsParent.SetActiveOptimized(showItem || nextSlotAvailable);
        }

        void SetVisibility(bool visible) {
            itemIcon.TrySetActiveOptimized(visible);
            quantityParent.SetActiveOptimized(visible);
            useStaticPrompt.SetActiveOptimized(visible);
        }
        
        protected override void OnDiscard() {
            _iconReference?.Release();
            _iconReference = null;
            
            for (int i = 0; i < _nextIconsReference.Length; i++) {
                _nextIconsReference[i]?.Release();
                _nextIconsReference[i] = null;
            }
        }
    }
}
