using Awaken.TG.Main.Crafting.Fireplace;
using Awaken.TG.Main.Locations.Actions.Attachments;
using Awaken.TG.Main.Locations.Attachments;
using Awaken.TG.MVC;
using Awaken.Utility;

namespace Awaken.TG.Main.Locations {
    public partial class StartWyrdRepellingFireplaceAction : StartFireplaceBaseAction, IRefreshedByAttachment<StartWyrdrepellingFireplaceAttachment> {
        public override ushort TypeForSerialization => SavedModels.StartWyrdRepellingFireplaceAction;

        StartWyrdrepellingFireplaceAttachment _spec;
        
        protected override bool ManualRestTime => _spec.ManualRestTime;

        public void InitFromAttachment(StartWyrdrepellingFireplaceAttachment spec, bool isRestored) {
            _cookingTabSetConfig = spec.TabSetSetConfig;
            _alchemyTabSetConfig = spec.AlchemyTabSetSetConfig;
            _spec = spec;
        }

        protected override void InitUI() {
            var fireplace = World.Any<WyrdRepellingFireplaceUI>();
            if (fireplace == null) {
                fireplace = World.Add(new WyrdRepellingFireplaceUI(_cookingTabSetConfig, _alchemyTabSetConfig, _spec.ManualRestTime, _spec.ForedwellerLocationTemplate, _spec.ForedwellerDialogue, _spec.ForedwellerDialogueTester, ParentModel, _spec.IsUpgraded));
            } else {
                fireplace.View<VWyrdRepellingFireplaceUI>().RefreshActions();
            }
            
            fireplace.ListenToLimited(Events.AfterDiscarded, () => EndFireplaceInteraction(false), this);
        }
    }
}