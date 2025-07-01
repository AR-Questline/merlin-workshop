using System;
using Awaken.TG.Main.General.NewThings;
using Awaken.TG.Main.Heroes.CharacterSheet.Items.Panel.Slot;
using Awaken.TG.Main.Heroes.Items;
using Awaken.TG.Main.Heroes.Items.Attachments;
using Awaken.TG.Main.Heroes.Items.Loadouts;
using Awaken.TG.Main.Localization;
using Awaken.TG.Main.UI.Components.Navigation;
using Awaken.TG.Main.Utility.UI;
using Awaken.TG.MVC;
using Awaken.TG.MVC.UI;
using Awaken.TG.MVC.UI.Events;
using Awaken.TG.Utility;
using TMPro;
using UnityEngine;

namespace Awaken.TG.Main.Heroes.CharacterSheet.Items.Loadouts {
    public class VCLoadoutSlot : ViewComponent<LoadoutsUI>, IUIAware, IEquipmentSlot, INewThingContainer {
        [SerializeField] ItemSlotUI slot;
        [SerializeField] bool main;
        [SerializeField] TextMeshProUGUI handText;
        
        [Space(10f)] 
        [SerializeField] ExplicitComponentNavigation navigation;

        VCLoadout _loadout;

        public bool Locked => Loadout.IsSlotLocked(Type);
        public bool AllowUnequip => ItemInSlot != null;
        public int LoadoutIndex => _loadout.LoadoutIndex;
        public EquipmentSlotType Type => main ? EquipmentSlotType.MainHand : _loadout.IsRanged ? EquipmentSlotType.Quiver : EquipmentSlotType.OffHand;
        
        public Item ItemInSlot => Loadout[Type];
        public HeroLoadout Loadout => Target.HeroItems.LoadoutAt(LoadoutIndex);
        public event Action onNewThingRefresh;
        
        public void Init(VCLoadout loadout) {
            _loadout = loadout;
        }
        
        protected override void OnAttach() {
            handText.SetText(main ? LocTerms.UILoadoutRightHand.Translate() : LocTerms.UILoadoutLeftHand.Translate());
            
            slot.SetVisibilityConfig(ItemSlotUI.VisibilityConfig.Equipment);

            slot.OnHoverStarted += OnHoverStarted;
            slot.OnHoverEnded += OnHoverEnded;
            
            Target.ListenTo(Model.Events.AfterChanged, Refresh, this);
            Refresh();
            World.Services.Get<NewThingsTracker>().RegisterContainer(this);
        }

        public void Equip(Item item) {
            if (item.Locked) {
                return;
            }
            
            Target.HeroItems.LoadoutAt(LoadoutIndex).Unequip(item);
            Target.HeroItems.LoadoutAt(LoadoutIndex).EquipItem(Type, item);
            RewiredHelper.VibrateHighFreq(VibrationStrength.Medium, VibrationDuration.VeryShort);
        }

        public void Unequip() {
            HeroLoadout loadout = Target.HeroItems.LoadoutAt(LoadoutIndex);
            if (!loadout.IsEquipped && ItemInSlot.Owner is Hero hero) {
                ItemInSlot.TryGetElement<ItemEquip>()?.PlayEquipToggleSound(hero, false);
            }
            loadout.EquipItem(Type, null);
            slot.ForceRefresh(null);
            RewiredHelper.VibrateLowFreq(VibrationStrength.Medium, VibrationDuration.VeryShort);
        }

        void Refresh() {
            Item item = ItemInSlot;
            if (item is {HiddenOnUI: true}) {
                slot.Setup(null, ParentView);

                if (item is {Locked: true}) {
                    slot.SetupOnlyIcon(item, ParentView);
                }
            } else {
                slot.Setup(item, ParentView);
            }
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
            return model is Item { HiddenOnUI: false, Owner: Hero } item && Type.Accept(item, Loadout);
        }
        
        public void RefreshNewThingsContainer() {
            onNewThingRefresh?.Invoke();
        }

        public UIResult Handle(UIEvent evt) {
            if (navigation.TryHandle(evt, out var result)) return result;

            if (evt is UIEPointTo) {
                slot.NotifyHover();
                return UIResult.Accept;
            }

            return UIResult.Ignore;
        }
        
        protected override void OnDiscard() {
            World.Services.Get<NewThingsTracker>().UnregisterContainer(this);
            base.OnDiscard();
        }
    }
}