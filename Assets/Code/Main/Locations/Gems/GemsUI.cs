using Awaken.TG.Main.Localization;
using Awaken.TG.Main.UI.ButtonSystem;
using Awaken.TG.Main.UI.Components.Tabs;
using Awaken.TG.Main.UI.EmptyContent;
using Awaken.TG.Main.UI.HUD;
using Awaken.TG.Main.Utility;
using Awaken.TG.MVC;
using Awaken.TG.MVC.Domains;
using Awaken.TG.MVC.UI.Handlers.States;
using Awaken.TG.MVC.UI.Universal;
using Awaken.TG.Utility;
using UnityEngine;

namespace Awaken.TG.Main.Locations.Gems {
    public partial class GemsUI : Model, IUIStateSource, IPromptHost, IClosable, GemsUITabs.ITabParent<VGemsUI> {
        public override Domain DefaultDomain => Domain.Gameplay;
        public sealed override bool IsNotSaved => true;
        public UIState UIState => UIState.ModalState(HUDState.MiddlePanelShown | HUDState.CompassHidden);

        public GemsUITabType CurrentType { get; set; } = GemsUITabType.GemManagement;
        public Tabs<GemsUI, VGemsUITabs, GemsUITabType, IGemsUITab> TabsController { get; set; }
        public Transform PromptsHost => View.PromptsHostFooter;
        public Prompts Prompts => Element<Prompts>();
        public Transform TooltipParent => View.TooltipParent;
        public Transform TooltipParentStatic => View.TooltipParentStatic;

        VGemsUI View => View<VGemsUI>();

        protected override void OnInitialize() {
            World.SpawnView<VModalBlocker>(this);
            World.SpawnView(this, CurrentType.ViewType, true);
        }

        protected override void OnFullyInitialized() {
            var prompts = AddElement(new Prompts(this));
            prompts.AddPrompt(Prompt.Tap(KeyBindings.UI.Generic.Cancel, LocTerms.UIGenericBack.Translate(), Close, Prompt.Position.Last), this);
            AddElement(new GemsUITabs());
        }

        public static GemsUI OpenSharpeningUI() {
            var gemsUI = new GemsUI();
            gemsUI.CurrentType = GemsUITabType.Sharpening;
            return World.Add(gemsUI);
        }

        public static GemsUI OpenIdentifyUI() {
            var gemsUI = new GemsUI();
            gemsUI.CurrentType = GemsUITabType.Identify;
            return World.Add(gemsUI);
        }

        public static GemsUI OpenGemsUI() {
            var gemsUI = new GemsUI();
            return World.Add(gemsUI);
        }

        public static GemsUI OpenGemsUI(GemsUITabType type) {
            var gemsUI = new GemsUI();
            gemsUI.CurrentType = type;
            return World.Add(gemsUI);
        }
        
        public void ShowEmptyInfo(bool contentActive, string title = null, string description = null) {
            View.EmptyInfoView.SetupLabels(title, description);
            this.Trigger(IEmptyInfo.Events.OnEmptyStateChanged, contentActive);
        }

        public void Close() {
            Discard();
        }
    }
}