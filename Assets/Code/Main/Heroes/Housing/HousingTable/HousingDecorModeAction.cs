using Awaken.TG.Main.Fights;
using Awaken.TG.Main.Heroes.Interactions;
using Awaken.TG.Main.Localization;
using Awaken.TG.Main.Locations.Actions;
using Awaken.TG.Main.UI.Housing;
using Awaken.TG.MVC;
using Awaken.TG.Utility;
using Awaken.Utility;

namespace Awaken.TG.Main.Heroes.Housing.HousingTable {
    public partial class HousingDecorModeAction : AbstractLocationAction, IDisableHeroActions {
        public override ushort TypeForSerialization => SavedModels.HousingDecorModeAction;

        readonly string _enableName = LocTerms.Enable.Translate();
        readonly string _disableName = LocTerms.Disable.Translate();

        static bool IsEnabled => World.HasAny<DecorMode>();
        
        public override string DefaultActionName => LocTerms.HousingDecorMode.Translate();
        public override InfoFrame ActionFrame => new(DefaultActionName, false);
        public override InfoFrame InfoFrame1 => new(IsEnabled ? _disableName : _enableName, true);
        
        public bool HeroActionsDisabled(IHeroAction heroAction) {
            if (heroAction is HousingDecorModeAction) {
                return false;
            }
            
            return heroAction is not FurnitureSlotAction && IsEnabled;
        }

        public override ActionAvailability GetAvailability(Hero hero, IInteractableWithHero interactable) {
            return hero.IsInCombat() ? ActionAvailability.Disabled : ActionAvailability.Available;
        }

        protected override void OnStart(Hero hero, IInteractableWithHero interactable) {
            if (IsEnabled) {
                World.Only<DecorMode>().Discard();
            } else {
                World.Add(new DecorMode());
            }
            
            World.Any<HeroInteractionUI>()?.TriggerChange();
        }
    }
}