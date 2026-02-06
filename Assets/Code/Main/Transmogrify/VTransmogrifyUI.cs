using Awaken.TG.Main.Heroes;
using Awaken.TG.Main.Heroes.CharacterSheet.Items.Panel.List;
using Awaken.TG.Main.Heroes.Housing;
using Awaken.TG.Main.Locations.Gems;
using Awaken.TG.Main.UI.ButtonSystem;
using Awaken.TG.Main.Utility.UI;
using Awaken.TG.MVC;
using Awaken.TG.MVC.Attributes;
using Awaken.Utility.Animations;
using Awaken.Utility.GameObjects;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Awaken.TG.Main.Transmogrify {
    [UsesPrefab("Transmogrify/" + nameof(VTransmogrifyUI))]
    public class VTransmogrifyUI : VGemBaseUI {
        [Title("Hosts")]
        [SerializeField] Transform chooseHost;
        [SerializeField] CanvasGroup leftCanvasGroup;
        [SerializeField] Transform rotatorHost;
        [SerializeField] GameObject costParent;
        
        [Title("Prompts")]
        [SerializeField] VGenericPromptUI clearPrompt;
        [SerializeField] VGenericPromptUI removePrompt;
        [SerializeField] VGenericPromptUI changePrompt;

        public VGenericPromptUI ClearPrompt => clearPrompt;
        public VGenericPromptUI RemovePrompt => removePrompt;
        public VGenericPromptUI ChangePrompt => changePrompt;
        public Transform ChooseHost => chooseHost;
        public Transform RotatorHost => rotatorHost;

        public static bool IsHomeHandcraftingStation => World.HasAny<FurnitureSlot>();
        
        VCItemSorting ItemSortingView => _itemSortingView ? _itemSortingView : leftCanvasGroup.GetComponentInChildren<VCItemSorting>();
        VCItemSorting _itemSortingView;
        
        protected override void OnInitialize() {
            SetCostParentActive(false);
        }

        public void FadeLeftSide(float targetAlpha) {
            bool setActive = targetAlpha > 0.01f;

            if (setActive) {
                leftCanvasGroup.TrySetActiveOptimized(true);
                ItemSortingView.ChangeSortingPromptState(true, true);
            }
            
            leftCanvasGroup.DOCanvasFade(targetAlpha, UITweens.FadeDuration)
                .onComplete = () => {
                if (!setActive) {
                    leftCanvasGroup.TrySetActiveOptimized(false);
                    ItemSortingView.ChangeSortingPromptState(false, false);
                } else if (RewiredHelper.IsGamepad) {
                        var index = Target.ItemsUI.ClickedItemsListElement.Index;
                        Target.ItemsUI.Element<ItemsListUI>().FocusGamepadAtElementAtIndex(index);
                }
            };
        }
        
        public void SetCostParentActive(bool active) {
            costParent.SetActive(active);
        }

        protected override IBackgroundTask OnDiscard() {
            return new BackgroundUniTask(Hero.Current.VHeroController.TryReloadBodyWithEquips());
        }
    }
}
