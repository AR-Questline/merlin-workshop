using Awaken.TG.Main.Fights.NPCs;
using Awaken.TG.Main.Heroes.Combat;
using Awaken.TG.Main.Saving;
using Awaken.TG.MVC;
using Awaken.TG.MVC.Elements;
using Awaken.TG.MVC.Events;

namespace Awaken.TG.Main.Maps.Compasses {
    /// <summary>
    /// Marker element for hiding Npc marker on compass.
    /// </summary>
    public partial class HideCompassMarker : Element<NpcElement> {
        public sealed override bool IsNotSaved => true;

        public new static class Events {
            public static readonly Event<NpcElement, NpcElement> HideCompassChanged = new(nameof(HideCompassChanged));
        }

        protected override void OnInitialize() {
            ParentModel.Trigger(Events.HideCompassChanged, ParentModel);
        }

        protected override void OnDiscard(bool fromDomainDrop) {
            if (ParentModel.HasBeenDiscarded) {
                return;
            }

            ParentModel.Trigger(Events.HideCompassChanged, ParentModel);
        }
    }

    public partial class ChangeSceneHideCompassMarker : HideCompassMarker {}
}