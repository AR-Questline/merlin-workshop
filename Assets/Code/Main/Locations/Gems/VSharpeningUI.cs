using System.Collections.Generic;
using System.Linq;
using Awaken.TG.Main.Fights.Utils;
using Awaken.TG.Main.Heroes.CharacterSheet.Items.Panel.List;
using Awaken.TG.Main.Heroes.CharacterSheet.Items.Panel.Slot;
using Awaken.TG.Main.Heroes.Items;
using Awaken.TG.Main.Localization;
using Awaken.TG.Main.Templates;
using Awaken.TG.MVC;
using Awaken.TG.MVC.Attributes;
using Awaken.TG.Utility;
using Cysharp.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.Pool;

namespace Awaken.TG.Main.Locations.Gems {
    [UsesPrefab("Gems/" + nameof(VSharpeningUI))]
    public class VSharpeningUI : VGemBaseUI {
        [SerializeField] protected Transform ingredientsHost;
        [SerializeField] protected TextMeshProUGUI requiredIngredientText;
        [SerializeField] protected VCIngredientUpgradeSlotUI ingredientItemSlot;
        [SerializeField] protected Material grayscaleMaterial;

        bool _delayCancellation;
        readonly List<VCIngredientUpgradeSlotUI> _ingredientItemSlots = new();
        ObjectPool<VCIngredientUpgradeSlotUI> _ingredientsSlotsPool;
        Item _items;
        
        protected override void OnInitialize() {
            requiredIngredientText.text = LocTerms.SharpeningRequiredIngredients.Translate();
            _ingredientsSlotsPool = new ObjectPool<VCIngredientUpgradeSlotUI>(
                createFunc: () => Instantiate(ingredientItemSlot, ingredientsHost),
                actionOnGet: slot => slot.gameObject.SetActive(true),
                actionOnRelease: slot => slot.gameObject.SetActive(false),
                actionOnDestroy: slot => Destroy(slot.gameObject),
                defaultCapacity: 1
            );
        }

        protected override void OnClickedItemChanged(Item item) {
            base.OnClickedItemChanged(item);
            RefreshRequiredIngredients();
        }

        protected override void OnActionPerformed() {
            RefreshRequiredIngredients();
        }

        protected override void FadeOutRightSide() {
            AsyncFadeOutRightSide().Forget();
        }

        async UniTaskVoid AsyncFadeOutRightSide() {
            base.FadeOutRightSide();
            _delayCancellation = false;
            // Unity shadergraph does not support alpha fade out until 2023.2, so we have to do it manually by just hiding the element
            if (!await AsyncUtil.DelayTime(this, FadeDuration / 3) || _delayCancellation) {
                return;
            }
            ClearIngredients();
        }

        void ClearIngredients() {
            foreach (VCIngredientUpgradeSlotUI itemSlot in _ingredientItemSlots) {
                itemSlot.ResetSlotIngredient();
                _ingredientsSlotsPool.Release(itemSlot);
            }
            
            _ingredientItemSlots.Clear();
        }
        
        void RefreshRequiredIngredients() {
            ClearIngredients();
            var ingredients = Target.Ingredients;
            if (ingredients == null) {
                return;
            }
            
            _delayCancellation = true;
            int idx = 0;
            foreach ((ItemTemplate itemTemplate, int quantity) in ingredients) {
                _ingredientsSlotsPool.Get(out VCIngredientUpgradeSlotUI ingredientSlot);
                _ingredientItemSlots.Add(ingredientSlot);
                var itemSlot = ingredientSlot.ItemSlotUI;
                var item = new Item(itemTemplate, quantity) {
                    MarkedNotSaved = true
                };
                World.Add(item);
                
                ingredientSlot.AssignItem(item, Target.IngredientTooltipUI);
                int inventoryQuantity = Target.SimilarItemsData.FirstOrDefault(x => x.Template.InheritsFrom(itemTemplate)).Quantity;
                ingredientSlot.RefreshRequiredQuantity(inventoryQuantity, quantity);
                ingredientSlot.transform.SetAsLastSibling();
                
                itemSlot.Setup(item, this, ItemDescriptorType.ExistingItem);
                itemSlot.SetVisibilityConfig(ItemSlotUI.VisibilityConfig.GearUpgrade);
                
                bool heroHasIngredient = Target.SimilarItemsData.Any(x => x.Template.InheritsFrom(itemTemplate) && x.Quantity >= quantity);
                itemSlot.SetIconMaterial(heroHasIngredient ? null : grayscaleMaterial);
                
                itemSlot.transform.SetSiblingIndex(idx);
                idx++;
            }

            if (Target.ItemsUI.TryGetElement<ItemsListUI>(out var itemsList)) {
                foreach (var itemElement in itemsList.Elements<ItemsListElementUI>()) {
                    itemElement.NextFocusTarget = () => _ingredientItemSlots.FirstOrDefault()?.FocusTarget;
                }
            }
        }
        
        protected void OnDestroy() {
            _ingredientsSlotsPool.Dispose();
        }
    }
}