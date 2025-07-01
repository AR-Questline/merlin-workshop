using Awaken.TG.Main.Locations;
using Awaken.TG.Main.Stories;
using Awaken.TG.Main.UI.Housing.FurnitureSlotOverview;
using UnityEngine;

namespace Awaken.TG.Main.Heroes.Housing {
    public partial class HeroHousingInvolvement : HeroInvolvement<FurnitureSlotOverviewUI> {
        public sealed override bool IsNotSaved => true;
        public override Location FocusedLocation => null;

        public override bool TryGetFocus(out Transform focus) {
            focus = ParentModel.FurnitureSlot.FurnitureLookAtTarget;
            return true;
        }
    }
}