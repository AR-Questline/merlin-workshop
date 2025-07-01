using Awaken.TG.Main.Locations.Actions;
using Awaken.TG.MVC;
using Awaken.TG.MVC.Attributes;
using UnityEngine;

namespace Awaken.TG.Main.Locations.Containers {
    [UsesPrefab("Locations/VHeroInventoryTransfer")]
    public class VHeroTransferItems : View<HeroTransferItems> {
        [UnityEngine.Scripting.Preserve] public Transform itemParent;

        public override Transform DetermineHost() => Target.ParentModel.View<VTransferItems>().heroInventoryParent;
    }
}