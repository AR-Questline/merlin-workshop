using Awaken.TG.Main.Localization;
using Awaken.TG.Main.Locations.Shops.Tabs;
using Awaken.TG.Main.UI.ButtonSystem;
using Awaken.TG.Main.UI.Components.Tabs;
using Awaken.TG.Main.UI.HUD;
using Awaken.TG.Main.Utility;
using Awaken.TG.MVC;
using Awaken.TG.MVC.Attributes;
using Awaken.TG.MVC.Elements;
using Awaken.TG.MVC.UI.Handlers.States;
using Awaken.TG.MVC.UI.Universal;
using Awaken.TG.Utility;
using UnityEngine;

namespace Awaken.TG.Main.Locations.Shops.UI {
    [SpawnsView(typeof(VModalBlocker), false)]
    public partial class ShopUI : Element<Shop>, IClosable, IUIStateSource, IPromptHost, ShopUITabs.ITabParent<VShopUI> {
        public sealed override bool IsNotSaved => true;

        Transform _host;
        VShopUI View => View<VShopUI>();
        
        public Transform TooltipParent => View.TooltipParent;
        public UIState UIState => UIState.ModalState(HUDState.MiddlePanelShown | HUDState.CompassHidden).WithPauseTime();
        public Shop Shop => ParentModel;
        [UnityEngine.Scripting.Preserve] public string MerchantName => ParentModel.ParentModel.DisplayName;
        public Transform TabButtonsHost => View.TabButtonsHost;
        public Transform ContentHost => View.ContentHost;
        public ShopUITabType CurrentType { get; set; } = ShopUITabType.Buy;
        public Tabs<ShopUI, VShopUITabs, ShopUITabType, IShopUITab> TabsController { get; set; }
        public Prompts Prompts => Element<Prompts>();
        public Transform PromptsHost => View.PromptsHost;

        // === Creation and initialization

        protected override void OnFullyInitialized() {
            World.SpawnView<VShopUI>(this, true);
            
            var prompts = AddElement(new Prompts(this));
            prompts.AddPrompt(Prompt.Tap(KeyBindings.UI.Generic.Cancel, LocTerms.UIGenericBack.Translate(), Close, Prompt.Position.Last), this);
            
            AddElement(new ShopUITabs());
        }

        public void Close() {
            Discard();
        }
    }
}