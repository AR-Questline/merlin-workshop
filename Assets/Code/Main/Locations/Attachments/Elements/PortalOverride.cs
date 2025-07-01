using Awaken.Utility;
using System.Linq;
using Awaken.TG.Main.Heroes;
using Awaken.TG.Main.Locations.Attachments.Attachment;
using Awaken.TG.Main.UI.Popup;
using Awaken.TG.Main.Utility.Tags;
using Awaken.TG.MVC.Elements;

namespace Awaken.TG.Main.Locations.Attachments.Elements {
    public partial class PortalOverride : Element<Portal>, IRefreshedByAttachment<PortalOverrideAttachment>, IPortalOverride {
        public override ushort TypeForSerialization => SavedModels.PortalOverride;

        FlagLogic _flagLogic;
        LocationReference _alternativePortal;

        PopupUI _popup;
        
        public bool Override => _flagLogic.Get(false);

        public void InitFromAttachment(PortalOverrideAttachment spec, bool isRestored) {
            _flagLogic = spec.FlagLogic;
            _alternativePortal = spec.AlternativePortal;
        }

        public void Execute(Hero hero) {
            _alternativePortal.MatchingLocations(null)
                .FirstOrDefault()
                ?.TryGetElement<Portal>()
                ?.Execute(hero);
        }
    }
}