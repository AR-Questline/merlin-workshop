using Awaken.TG.Main.Crafting;
using Awaken.TG.Main.Crafting.Cooking;
using Awaken.TG.Main.UI.HUD;
using Awaken.TG.MVC;
using Awaken.TG.MVC.Attributes;
using Awaken.TG.MVC.Domains;
using Awaken.TG.MVC.UI.Handlers.States;

namespace Awaken.TG.Main.Locations.Gems.Upgrades {
    [SpawnsView(typeof(VUpgradesUI))]
    public partial class UpgradesUI : Model, IClosable, IUIStateSource {
        public override Domain DefaultDomain => Domain.Gameplay;
        public sealed override bool IsNotSaved => true;
        public UIState UIState => UIState.ModalState(HUDState.MiddlePanelShown | HUDState.CompassHidden).WithPauseTime();
        
        TabSetConfig _tabSetConfig;
        
        public UpgradesUI(TabSetConfig tabSetConfig) {
            _tabSetConfig = tabSetConfig;
        }
        
        public Model Craft() {
            return World.Add(new CraftingTabsUI(_tabSetConfig));
        }

        public Model Upgrade() {
            return GemsUI.OpenGemsUI(GemsUITabType.Sharpening);
        }
        
        public Model AddGem() {
            return GemsUI.OpenGemsUI(GemsUITabType.GemManagement);
        }
        
        public Model WeightReduction() {
            return GemsUI.OpenGemsUI(GemsUITabType.WeightReduction);
        }

        public void Close() {
            Discard();
        }
    }
}