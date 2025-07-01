using Awaken.Utility;
using System.Linq;
using Awaken.TG.Main.Crafting;
using Awaken.TG.Main.Crafting.Cooking;
using Awaken.TG.Main.Fights;
using Awaken.TG.Main.Heroes;
using Awaken.TG.Main.Heroes.Interactions;
using Awaken.TG.Main.Locations.Actions;
using Awaken.TG.Main.Locations.Actions.Attachments;
using Awaken.TG.Main.Locations.Attachments;
using Awaken.TG.Main.Locations.Gems.Upgrades;
using Awaken.TG.Main.VisualGraphUtils;
using Awaken.TG.MVC;

namespace Awaken.TG.Main.Locations {
    public partial class StartCraftingAction : AbstractLocationAction, IRefreshedByAttachment<StartCraftingAttachment> {
        public override ushort TypeForSerialization => SavedModels.StartCraftingAction;

        TabSetConfig _tabSetConfig;
        
        public TabSetConfig TabSetConfig => _tabSetConfig;

        public void InitFromAttachment(StartCraftingAttachment spec, bool isRestored) {
            _tabSetConfig = spec.TabSetConfig;
        }

        protected override void OnStart(Hero hero, IInteractableWithHero interactable) {
            if (_tabSetConfig.Dictionary.Keys.All(k => k == CraftingTabTypes.RecipeHandcrafting)) {
                World.Add(new UpgradesUI(_tabSetConfig));
            } else {
                VGUtils.ToggleCrafting(_tabSetConfig);
            }
        }

        public override ActionAvailability GetAvailability(Hero hero, IInteractableWithHero interactable) {
            return hero.IsInCombat() ? ActionAvailability.Disabled : base.GetAvailability(hero, interactable);
        }
    }
}