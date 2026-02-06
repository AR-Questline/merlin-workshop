using Awaken.Utility;
using System;
using Awaken.TG.Main.Fights;
using Awaken.TG.Main.Heroes;
using Awaken.TG.Main.Heroes.Interactions;
using Awaken.TG.Main.Locations.Actions.Attachments;
using Awaken.TG.Main.Locations.Attachments;
using Awaken.TG.Main.Stories;
using Awaken.TG.Main.UI.Popup;
using Awaken.TG.MVC;
using Awaken.TG.MVC.Events;

namespace Awaken.TG.Main.Locations.Actions {
    public partial class ReadAction : AbstractLocationAction, IRefreshedByAttachment<ReadAttachment> {
        public override ushort TypeForSerialization => SavedModels.ReadAction;

        StoryBookmark _readable;
        bool _hasImage;
        
        protected override bool DisableInCombat => true;
        
        public new static class Events {
            public static readonly Event<ReadAction, Story> StoryEnded = new(nameof(StoryEnded));
        }

        public void InitFromAttachment(ReadAttachment spec, bool isRestored) {
            _readable = spec.Readable;
            _hasImage = spec.HasImage;
        }

        protected override void OnStart(Hero hero, IInteractableWithHero interactable) {
            Type viewType = _hasImage ? typeof(VReadablePopupUI) : typeof(VReadableObjectPopupUI);
            if (_readable != null) {
                var story = Story.StartStory(StoryConfig.Interactable(interactable, _readable, viewType));
                if (!story?.HasBeenDiscarded ?? false) {
                    story.ListenTo(Model.Events.AfterDiscarded, _ => {
                        this.Trigger(Events.StoryEnded, story);
                    }, this);
                }
            }
        }

        public override ActionAvailability GetAvailability(Hero hero, IInteractableWithHero interactable) {
            return base.GetAvailability(hero, interactable);
        }
    }
}