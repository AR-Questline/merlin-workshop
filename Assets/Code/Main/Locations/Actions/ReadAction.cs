using Awaken.Utility;
using System;
using Awaken.TG.Main.Fights;
using Awaken.TG.Main.Fights.Factions;
using Awaken.TG.Main.Heroes;
using Awaken.TG.Main.Heroes.Interactions;
using Awaken.TG.Main.Locations.Actions.Attachments;
using Awaken.TG.Main.Locations.Attachments;
using Awaken.TG.Main.Stories;
using Awaken.TG.Main.Stories.Core;
using Awaken.TG.Main.UI.Popup;

namespace Awaken.TG.Main.Locations.Actions {
    public partial class ReadAction : AbstractLocationAction, IRefreshedByAttachment<ReadAttachment> {
        public override ushort TypeForSerialization => SavedModels.ReadAction;

        StoryBookmark _readable;
        bool _hasImage;

        public void InitFromAttachment(ReadAttachment spec, bool isRestored) {
            _readable = spec.Readable;
            _hasImage = spec.HasImage;
        }

        protected override void OnStart(Hero hero, IInteractableWithHero interactable) {
            Type viewType = _hasImage ? typeof(VReadablePopupUI) : typeof(VReadableObjectPopupUI);
            if (_readable != null) {
                Story.StartStory(StoryConfig.Interactable(interactable, _readable, viewType));
            }
        }

        public override ActionAvailability GetAvailability(Hero hero, IInteractableWithHero interactable) {
            return hero.IsInCombat() ? ActionAvailability.Disabled : base.GetAvailability(hero, interactable);
        }
    }
}