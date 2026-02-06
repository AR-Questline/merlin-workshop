using Awaken.TG.Main.UI.Components.Tabs;
using Awaken.TG.MVC;
using Awaken.TG.MVC.Attributes;
using Awaken.TG.MVC.UI.Handlers.Focuses;
using UnityEngine;

namespace Awaken.TG.Main.Heroes.Storage {
    [UsesPrefab("Storage/" + nameof(VHeroStorageUI))]
    public class VHeroStorageUI : VTabParent<HeroStorageUI>, IAutoFocusBase {
        [SerializeField] Transform promptsHost;
        [SerializeField] Transform tooltipParent;
        [SerializeField] Transform doublePromptsHost;

        public Transform PromptsHost => promptsHost;
        public Transform TooltipParent => tooltipParent;
        public Transform DoublePromptsHost => doublePromptsHost;
        
        public override Transform DetermineHost() => Services.Get<ViewHosting>().OnMainCanvas();
    }
}
