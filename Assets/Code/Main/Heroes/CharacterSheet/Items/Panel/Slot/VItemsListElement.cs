using Awaken.TG.Main.Heroes.CharacterSheet.Items.Panel.List;
using Awaken.TG.Main.Heroes.Items;
using Awaken.TG.MVC;
using Awaken.TG.MVC.Attributes;
using Awaken.TG.MVC.UI;
using Awaken.TG.MVC.UI.Events;
using Awaken.Utility.Debugging;

namespace Awaken.TG.Main.Heroes.CharacterSheet.Items.Panel.Slot {
    [UsesPrefab("Items/Slot/" + nameof(VItemsListElement))]
    public class VItemsListElement : RetargetableView<ItemsListElementUI>, IUIAware {
        Item Item => Target.Item;
        ItemSlotUI _slot;
        
        protected override void OnFirstInit() {
            _slot = GetComponent<ItemSlotUI>();
            _slot.SetVisibilityConfig(ItemSlotUI.VisibilityConfig.All);
        }

        protected override void OnOldTargetRemove() {
            _slot.OnSelected -= Target.OnSelected;
            _slot.OnDeselected -= Target.OnDeselected;
            _slot.OnHoverStarted -= Target.OnHoverStarted;
            _slot.OnHoverEnded -= OnHoverEnded;
        }

        protected override void OnNewTarget() {
            _slot.OnSelected += Target.OnSelected;
            _slot.OnDeselected += Target.OnDeselected;
            _slot.OnHoverStarted += Target.OnHoverStarted;
            _slot.OnHoverEnded += OnHoverEnded;
            
            Target.ListenTo(Model.Events.AfterChanged, Refresh, this);
            Item.ListenTo(Item.Events.QuantityChanged, Refresh, this);
            Item.ListenTo(Item.Events.ActionPerformed, Refresh, this);
            Refresh();
        }

        void OnHoverEnded() {
            _slot.ForceUnselect();
            _slot.ResetHoveredState();
            Target.OnHoverEnded();
        }

        void Refresh() {
            if (HasBeenDiscarded || Target.HasBeenDiscarded) return;
            _slot.Setup(Item, this, Target.ItemDescriptorType);
        }
        
        public void ForceRefresh() {
            _slot.ForceUnselect();
            _slot.ResetHoveredState();
        }
        
        public void ForceClick() {
            _slot.NotifyClick();
        }

        public UIResult Handle(UIEvent evt) {
            if (Target == null) {
                return UIResult.Ignore;
            }
            
            if (evt is UINaviAction naviAction) {
                switch (Target.ItemsListUI.ParentModel.Config.TabsPosition){
                    case LayoutPosition.Top or LayoutPosition.Bottom:
                        Target.ItemsListUI.HandelNavigation(naviAction, Target);
                        break;
                    case LayoutPosition.Left or LayoutPosition.Right:
                        Log.Minor?.Error("Vertical navigation is not supported for ItemsListElement");
                        break;
                }
                return UIResult.Accept;
            }

            if (evt is UISubmitAction or UIEMouseDown { IsLeft: true }) {
                _slot.NotifyClick();
                return UIResult.Accept;
            }

            if (evt is UIEPointTo) {
                _slot.NotifyHover();
                return UIResult.Accept;
            }
            
            return Target.HandleEvent(evt);
        }
    }
}