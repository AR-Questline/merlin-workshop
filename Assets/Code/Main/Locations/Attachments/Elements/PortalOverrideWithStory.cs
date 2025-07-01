using Awaken.TG.Main.Heroes;
using Awaken.TG.Main.Locations.Attachments.Attachment;
using Awaken.TG.Main.Stories;
using Awaken.TG.Main.UI.Popup;
using Awaken.TG.Main.Utility.Tags;
using Awaken.TG.MVC;
using Awaken.TG.MVC.Elements;
using Awaken.Utility;

namespace Awaken.TG.Main.Locations.Attachments.Elements {
    public partial class PortalOverrideWithStory : Element<Portal>, IRefreshedByAttachment<PortalOverrideWithStoryAttachment>, IPortalOverride {
        public override ushort TypeForSerialization => SavedModels.PortalOverrideWithStory;

        bool _requiresFlag;
        FlagLogic _flagLogic;
        StoryBookmark _alternativeStory;
        bool _triggerBasePortalOnStoryEnd;

        PopupUI _popup;
        
        public bool Override => !_requiresFlag || _flagLogic.Get(false);

        public void InitFromAttachment(PortalOverrideWithStoryAttachment spec, bool isRestored) {
            _requiresFlag = spec.RequiresFlag;
            _flagLogic = spec.FlagLogic;
            _alternativeStory = spec.AlternativeStory;
            _triggerBasePortalOnStoryEnd = spec.TriggerBasePortalOnStoryEnd;
        }

        public void Execute(Hero hero) {
            var story = Story.StartStory(StoryConfig.Interactable(ParentModel.ParentModel, _alternativeStory, typeof(VDialogue)));
            if (_triggerBasePortalOnStoryEnd) {
                if (story is { HasBeenDiscarded: false }) {
                    story.ListenTo(Events.AfterDiscarded, () => {
                        TriggerBasePortal(hero);
                    }, this);
                } else {
                    TriggerBasePortal(hero);
                }
            }
        }
        
        void TriggerBasePortal(Hero hero) {
            ParentModel.ExecuteInternal(hero);
        }
    }
}