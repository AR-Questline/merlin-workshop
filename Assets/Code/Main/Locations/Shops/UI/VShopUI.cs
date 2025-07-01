using Awaken.TG.Main.UI.Components.Tabs;
using Awaken.TG.MVC;
using Awaken.TG.MVC.Attributes;
using Awaken.TG.MVC.UI.Handlers.Focuses;
using UnityEngine;

namespace Awaken.TG.Main.Locations.Shops.UI {
    [UsesPrefab("Shop/VShopUI")]
    public class VShopUI : VTabParent<ShopUI>, IAutoFocusBase {
        [SerializeField] Transform promptsHost;
        [SerializeField] Transform tooltipParent;

        public Transform PromptsHost => promptsHost;
        public Transform TooltipParent => tooltipParent;
        
        public override Transform DetermineHost() => Services.Get<ViewHosting>().OnMainCanvas();
    }
}