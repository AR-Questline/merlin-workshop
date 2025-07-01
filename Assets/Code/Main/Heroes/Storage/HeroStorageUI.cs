using Awaken.TG.Main.Heroes.Items.Tooltips;
using Awaken.TG.Main.Heroes.Items.Tooltips.Views;
using Awaken.TG.Main.Localization;
using Awaken.TG.Main.UI.ButtonSystem;
using Awaken.TG.Main.UI.Components.Tabs;
using Awaken.TG.Main.UI.HUD;
using Awaken.TG.Main.UI.HUD.AdvancedNotifications.Item;
using Awaken.TG.Main.UI.HUD.AdvancedNotifications.LeftScreen.SpecialItem;
using Awaken.TG.Main.Utility;
using Awaken.TG.MVC;
using Awaken.TG.MVC.Domains;
using Awaken.TG.MVC.UI.Handlers.States;
using Awaken.TG.Utility;
using Awaken.Utility.Debugging;
using UnityEngine;

namespace Awaken.TG.Main.Heroes.Storage {
    public partial class HeroStorageUI : Model, IClosable, IUIStateSource, IPromptHost, HeroStorageTabs.ITabParent {
        public override Domain DefaultDomain => Domain.Gameplay;
        public sealed override bool IsNotSaved => true;
        public UIState UIState => UIState.ModalState(HUDState.MiddlePanelShown | HUDState.CompassHidden).WithPauseTime();
        
        public HeroStorage Storage { get; }
        float _creationTime;
        
        VHeroStorageUI View => View<VHeroStorageUI>();
        public Transform TabButtonsHost => View.TabButtonsHost;
        public Transform ContentHost => View.ContentHost;
        public HeroStorageTabType CurrentType { get; set; } = HeroStorageTabType.Put;
        public Tabs<HeroStorageUI, VHeroStorageTabs, HeroStorageTabType, HeroStorageTabUI> TabsController { get; set; }
        public Prompts Prompts => Element<Prompts>();
        public Transform PromptsHost => View.PromptsHost;
        Transform TooltipHost => View.TooltipParent;
        
        ItemTooltipUI _tooltip;

        public HeroStorageUI(HeroStorage storage) {
            Storage = storage;
        }

        protected override void OnFullyInitialized() {
            World.SpawnView<VHeroStorageUI>(this, true);
            
            var prompts = AddElement(new Prompts(this));
            prompts.AddPrompt(Prompt.Tap(KeyBindings.UI.Generic.Cancel, LocTerms.UIGenericBack.Translate(), () => TryClose(), Prompt.Position.Last), this);
            
            _tooltip = AddElement(new ItemTooltipUI(typeof(VBagItemTooltipSystemUI), TooltipHost, 0f, 0f, 0f, true, preventDisappearing: true));
            AddElement(new HeroStorageTabs());

            _creationTime = Time.unscaledTime;
            World.Only<ItemNotificationBuffer>().SuspendPushingNotifications = true;
            World.Only<SpecialItemNotificationBuffer>().SuspendPushingNotifications = true;
        }

        public void ForceTooltipDisappear() {
            _tooltip.ForceDisappear();
        }
        
        public void Close() {
            if (TryClose() == false) {
                Log.Important?.Error("Closing HeroStorageUI too soon");
            }
        }

        public bool TryClose() {
            if (Time.unscaledTime > _creationTime + 0.5f) {
                Discard();
                return true;
            }
            return false;
        }
        
        protected override void OnDiscard(bool fromDomainDrop) {
            World.Only<ItemNotificationBuffer>().SuspendPushingNotifications = false;
            World.Only<SpecialItemNotificationBuffer>().SuspendPushingNotifications = false;
        }
    }
}