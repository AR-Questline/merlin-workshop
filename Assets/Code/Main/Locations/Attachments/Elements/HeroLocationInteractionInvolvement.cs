using Awaken.TG.Main.Stories;
using UnityEngine;

namespace Awaken.TG.Main.Locations.Attachments.Elements {
    public partial class HeroLocationInteractionInvolvement : HeroInvolvement<Location> {
        public sealed override bool IsNotSaved => true;

        Location _focusedLocation;
        bool _hideHands;

        public override Location FocusedLocation => _focusedLocation;
        public override bool HideHands => _hideHands;

        public HeroLocationInteractionInvolvement(Location location, bool hideWeapons = true, bool hideHands = true) : base(hideWeapons) {
            _focusedLocation = location;
            _hideHands = hideHands;
        }

        public override bool TryGetFocus(out Transform focus) {
            if (base.TryGetFocus(out var baseFocus)) {
                focus = baseFocus;
                return true;
            }

            focus = FocusedLocation?.ViewParent;
            return focus != null;
        }

        public void ChangeFocusedLocation(Location newFocusedLocation = null) {
            _focusedLocation = newFocusedLocation ?? ParentModel;
            this.TriggerChange();
        }
    }
}
