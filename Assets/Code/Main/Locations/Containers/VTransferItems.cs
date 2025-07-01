using Awaken.TG.Main.Locations.Containers;
using Awaken.TG.MVC;
using Awaken.TG.MVC.Attributes;
using Awaken.TG.MVC.UI.Handlers.Focuses;
using UnityEngine;

namespace Awaken.TG.Main.Locations.Actions {
    [UsesPrefab("Locations/VTransferItems")]
    public class VTransferItems : View<TransferItems>, IAutoFocusBase {
        public Transform heroInventoryParent, containerParent;

        public override Transform DetermineHost() => Services.Get<ViewHosting>().OnMainCanvas();
    }
}