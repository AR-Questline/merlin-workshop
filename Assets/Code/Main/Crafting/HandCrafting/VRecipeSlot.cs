using Awaken.TG.Main.Crafting.Slots;
using Awaken.TG.Main.Heroes.CharacterSheet.Items.Panel.Slot;
using Awaken.TG.Main.Heroes.Items.Tooltips.Descriptors;
using Awaken.TG.Main.UI.Components;
using Awaken.TG.Main.Utility.UI;
using Awaken.TG.MVC;
using Awaken.TG.MVC.Attributes;
using Awaken.TG.MVC.UI;
using Awaken.TG.MVC.UI.Events;
using Awaken.TG.MVC.UI.Handlers.Focuses;
using Awaken.Utility.Animations;
using UnityEngine;

namespace Awaken.TG.Main.Crafting.HandCrafting {
    [UsesPrefab("Crafting/Handcrafting/" + nameof(VRecipeSlot))]
    public class VRecipeSlot : RetargetableView<RecipeSlot>, IUIAware {
        [SerializeField] ItemSlotUI itemSlotUI;
        [SerializeField] ARButton slotButton;
        [SerializeField] CanvasGroup contentCanvasGroup;
        
        public Component FocusTarget => slotButton;
        bool IsCraftable => Target.IsCraftable;
        
        TempItemDescriptor _tempItemDescriptor;
        
        protected override void OnNewTarget() {
            Target.ParentModel.ParentModel.ParentModel.ListenTo(Model.Events.AfterChanged, _ => {
                contentCanvasGroup.alpha = IsCraftable ? 1 : 0.2f;
            }, this);

            contentCanvasGroup.alpha = IsCraftable ? 1 : 0.2f;

            _tempItemDescriptor = new TempItemDescriptor(Target.Recipe, Target);
            itemSlotUI.SetVisibilityConfig(ItemSlotUI.VisibilityConfig.Crafting);
            itemSlotUI.Setup(_tempItemDescriptor.ExistingItem, this);
            slotButton.OnClick += OnClicked;
            itemSlotUI.OnDeselected += UnselectSlot;
            slotButton.ClearAllOnClickAudioFeedback();
            UnselectSlot();
        }
        
        protected override void OnOldTargetRemove() {
            slotButton.OnClick -= OnClicked;
            itemSlotUI.OnDeselected -= UnselectSlot;
        }
        
        void OnClicked() {
            slotButton.PlayClickAudioFeedback(IsCraftable, false);
            if (RewiredHelper.IsGamepad && Target.IsSelected) {
                World.Any<WorkbenchSlot>().TryFocusSlot();
            }
            Target.SelectSlot();
        }
            
        public void SelectSlot() {
            itemSlotUI.NotifyClick();
            World.Only<Focus>().Select(FocusTarget);
        }
        
        public void UnselectSlot() {
            itemSlotUI.ForceUnselect();
        }
        
        public UIResult Handle(UIEvent evt) {
            if (evt is UISubmitAction or UIEMouseDown { IsLeft: true }) {
                Target.SelectSlot();
                return UIResult.Accept;
            }
            
            if (evt is UIEPointTo) {
                itemSlotUI.NotifyHover();
                return UIResult.Accept;
            }
            
            return UIResult.Ignore;
        }

        protected override IBackgroundTask OnDiscard() {
            _tempItemDescriptor?.Dispose();
            return base.OnDiscard();
        }
    }
}