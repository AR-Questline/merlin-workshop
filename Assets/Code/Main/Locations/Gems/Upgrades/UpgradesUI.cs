using Awaken.TG.Main.Crafting;
using Awaken.TG.Main.Crafting.Cooking;
using Awaken.TG.Main.Saving;
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
        
        public void Craft() {
            World.Add(new CraftingTabsUI(_tabSetConfig));
        }

        public void Upgrade() {
            GemsUI.OpenGemsUI(GemsUITabType.Sharpening);
        }
        
        public void AddGem() {
            GemsUI.OpenGemsUI(GemsUITabType.GemManagement);
        }
        
        public void WeightReduction() {
            GemsUI.OpenGemsUI(GemsUITabType.WeightReduction);
        }
        
        public void UnlockEffect() {
            //GemsUI.OpenGemsUI(GemsUITabType.UnlockEffect);
        }

        public void Close() {
            Discard();
        }
    }
}