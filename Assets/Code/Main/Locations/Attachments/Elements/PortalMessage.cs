using Awaken.Utility;
using System.Linq;
using Awaken.TG.Main.Heroes;
using Awaken.TG.Main.Localization;
using Awaken.TG.Main.Locations.Attachments.Attachment;
using Awaken.TG.Main.Timing;
using Awaken.TG.Main.Timing.ARTime;
using Awaken.TG.Main.Timing.ARTime.Modifiers;
using Awaken.TG.Main.UI.Popup;
using Awaken.TG.MVC;
using Awaken.TG.MVC.Elements;

namespace Awaken.TG.Main.Locations.Attachments.Elements {
    public partial class PortalMessage : Element<Portal>, IRefreshedByAttachment<PortalMessageAttachment>, IPortalOverride {
        public override ushort TypeForSerialization => SavedModels.PortalMessage;

        LocString _title;
        LocString _message;
        bool _hasChoice;
        LocationReference _alternativePortal;
        
        PopupUI _popup;

        public bool Override => true;
        
        public void InitFromAttachment(PortalMessageAttachment spec, bool isRestored) {
            _title = spec.Title;
            _message = spec.Message;
            _hasChoice = spec.HasChoice;
            _alternativePortal = spec.AlternativePortal;
        }

        public void Execute(Hero hero) => ShowMessage(hero);
        
        public void ShowMessage(Hero hero) {
            World.Only<GlobalTime>().AddTimeModifier(new OverrideTimeModifier(ID, 0));
            _popup = null;
            var acceptPrompt = PopupUI.AcceptTapPrompt(() => {
                _popup?.Discard();
                World.Only<GlobalTime>().RemoveTimeModifiersFor(ID);
                ParentModel.ExecuteInternal(hero);
            });

            if (_hasChoice) {
                var declinePrompt = PopupUI.CancelTapPrompt(() => {
                    _alternativePortal.MatchingLocations(null)
                        .FirstOrDefault()
                        ?.TryGetElement<Portal>()
                        ?.Execute(hero);

                    _popup?.Discard();
                    World.Only<GlobalTime>().RemoveTimeModifiersFor(ID); 
                });

                _popup = PopupUI.SpawnSimplePopup(typeof(VSmallPopupUI), _message, declinePrompt, acceptPrompt, _title);
            } else {
                _popup = PopupUI.SpawnNoChoicePopup(typeof(VSmallPopupUI), _title, _message, acceptPrompt);
            }
        }
    }
}