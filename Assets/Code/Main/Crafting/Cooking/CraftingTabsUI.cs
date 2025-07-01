using System.Collections.Generic;
using System.Linq;
using Awaken.TG.Main.Localization;
using Awaken.TG.Main.UI.ButtonSystem;
using Awaken.TG.Main.UI.Components.Tabs;
using Awaken.TG.Main.UI.HUD;
using Awaken.TG.Main.Utility;
using Awaken.TG.MVC;
using Awaken.TG.MVC.Domains;
using Awaken.TG.MVC.Events;
using Awaken.TG.MVC.UI.Handlers.States;
using Awaken.TG.Utility;
using UnityEngine;

namespace Awaken.TG.Main.Crafting.Cooking {
    public partial class CraftingTabsUI : Model, IUIStateSource, IClosable, IPromptHost, CraftingTabs.ITabParent<VCraftingTabsUI> {
        public sealed override Domain DefaultDomain => Domain.Gameplay;
        public sealed override bool IsNotSaved => true;

        public bool FullyCreated { get; private set; }
        public UIState UIState => UIState.ModalState(HUDState.MiddlePanelShown | HUDState.CompassHidden).WithPauseTime();
        public Transform PromptsHost { get; private set; }
        public Prompts Prompts => TryGetElement<Prompts>();

        public Transform TabButtonsHost { get; private set; }
        public Transform ContentHost { get; private set; }
        public CraftingTabTypes CurrentType { get; set; }
        public Tabs<CraftingTabsUI, VCraftingTabs, CraftingTabTypes, ICraftingTabContents> TabsController { get; set; }

        public IEnumerable<CraftingTabTypes> Tabs => _tabSetConfig.Dictionary.Keys;

        readonly TabSetConfig _tabSetConfig;

        // === Events
        public new static class Events {
            public static readonly Event<CraftingTabsUI, bool> CraftingTabsInitialized = new(nameof(CraftingTabsInitialized));
        }
        
        public CraftingTabsUI(TabSetConfig tabSetConfig) {
            _tabSetConfig = tabSetConfig;
        }

        protected override void OnFullyInitialized() {
            AddElement(new Prompts(this));
            var view = World.SpawnView<VCraftingTabsUI>(this, true);
            PromptsHost = view.PromptHost;
            TabButtonsHost = view.TabButtonsHost;
            ContentHost = view.ContentHost;
            CraftingTabs tabs = AddElement(new CraftingTabs(Tabs));
            InitPrompts();
        }

        void InitPrompts() {
            Prompts.AddPrompt(Prompt.Tap(KeyBindings.UI.Generic.Cancel, LocTerms.Close.Translate(), Close, Prompt.Position.Last), this);
            this.Trigger(Events.CraftingTabsInitialized, true);
            FullyCreated = true;
        }
        
        public CraftingTemplate TemplateFromType() => _tabSetConfig.Dictionary[CurrentType];

        public void Close() => Discard();

        public bool TabShouldBeActive(CraftingTabTypes type) => Tabs.Contains(type);
    }
}
