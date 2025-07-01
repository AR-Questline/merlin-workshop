using System;
using Awaken.TG.Main.Character;
using Awaken.TG.Main.General.NewThings;
using Awaken.TG.Main.Heroes.CharacterSheet.Items.Loadouts;
using Awaken.TG.Main.Heroes.CharacterSheet.Items.Panel.Slot;
using Awaken.TG.Main.Heroes.Items;
using Awaken.TG.Main.UI.Components.Navigation;
using Awaken.TG.Main.Utility.UI;
using Awaken.TG.MVC;
using Awaken.TG.MVC.UI;
using Awaken.TG.MVC.UI.Events;
using UnityEngine;

namespace Awaken.TG.Main.Heroes.CharacterSheet.Items.Equipment {
    public abstract class VCEquipmentSlotBase : ViewComponent<LoadoutsUI>, IUIAware, IEquipmentSlot, INewThingContainer {
        [SerializeField] ItemSlotUI slot;
        [SerializeField] ExplicitComponentNavigation navigation;

        public abstract bool Hidden { get; }
        public abstract bool Locked { get; }
        public abstract EquipmentSlotType Type { get; }
        
        HeroItems HeroItems => Hero.Current.HeroItems;
        public bool AllowUnequip => HeroItems.EquippedItem(Type) != null;
        public Item ItemInSlot => HeroItems.EquippedItem(Type);
        public event Action onNewThingRefresh;

        protected override void OnAttach() {
            slot.SetVisibilityConfig(Hidden ? new ItemSlotUI.VisibilityConfig() : ItemSlotUI.VisibilityConfig.Equipment);
            
            slot.OnHoverStarted += OnHoverStarted;
            slot.OnHoverEnded += OnHoverEnded;

            Target.ListenTo(Model.Events.AfterChanged, Refresh, this);
            World.Services.Get<NewThingsTracker>().RegisterContainer(this);
        }

        public void Equip(Item item) {
            if (item.IsEquipped) {
                return;
            }
            
            Hero.Current.HeroItems.Unequip(item);
            Hero.Current.HeroItems.Equip(item, Type);
            RewiredHelper.VibrateHighFreq(VibrationStrength.Medium, VibrationDuration.VeryShort);
            Refresh();
        }

        public void Unequip() {
            Hero.Current.HeroItems.Unequip(Type);
            slot.ForceRefresh(null);
            RewiredHelper.VibrateLowFreq(VibrationStrength.Medium, VibrationDuration.VeryShort);
        }

        void Refresh() {
            slot.Setup(ItemInSlot, ParentView);
        }

        void OnHoverStarted() {
            if (Target is { HasBeenDiscarded: false }) {
                Target.CurrentlyHoveredSlot.OnStartHover(this);
            }
        }

        void OnHoverEnded() {
            if (Target is { HasBeenDiscarded: false }) {
                Target.CurrentlyHoveredSlot.OnStopHover(this);
            }
        }
        
        public bool NewThingBelongsToMe(IModel model) {
            return model is Item { HiddenOnUI: false, Owner: Hero } item && Type.Accept(item);
        }
        
        public void RefreshNewThingsContainer() {
            onNewThingRefresh?.Invoke();
        }

        public UIResult Handle(UIEvent evt) {
            if (Target == null || Target.HasBeenDiscarded) {
                return UIResult.Ignore;
            }
            
            if (TryHandleNavigationSkippingHidden(evt, out var result)) return result;
            
            if (evt is UIEPointTo) {
                slot.NotifyHover();
                return UIResult.Accept;
            }

            return UIResult.Ignore;
        }
        
        bool TryHandleNavigationSkippingHidden(UIEvent evt, out UIResult result) {
            if (evt is not UINaviAction navi) {
                result = default;
                return false;
            }

            VCEquipmentSlotBase skippedToSlot = this;
            const int MaxSteps = 3;
            
            for (int i = 0; i < MaxSteps; i++) {
                var target = skippedToSlot.navigation.GetNavigationTarget(navi.direction);
                if (!target.TryGetComponent(out VCEquipmentSlotBase targetSlot) || !targetSlot.Hidden) {
                    return skippedToSlot.navigation.TryHandle(evt, out result);
                }
                skippedToSlot = targetSlot;
            }
            
            return navigation.TryHandle(evt, out result);
        }

        protected override void OnDiscard() {
            World.Services.Get<NewThingsTracker>().UnregisterContainer(this);
            base.OnDiscard();
        }
    }
}