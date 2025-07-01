using Awaken.TG.Main.UI.Components.Tabs;
using Awaken.TG.Main.UI.EmptyContent;
using Awaken.TG.MVC;
using Awaken.TG.MVC.Attributes;
using Awaken.TG.MVC.UI.Handlers.Focuses;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Awaken.TG.Main.Locations.Gems {
    [UsesPrefab("Gems/VGemsUI")]
    public class VGemsUI : VTabParent<GemsUI>, IAutoFocusBase, IEmptyInfo {
        [field: SerializeField] public Transform PromptsHostFooter { get; private set; }
        [field: SerializeField] public Transform TooltipParentStatic { get; private set; }
        [field: SerializeField] public Transform TooltipParent { get; private set; }

        [Title("Empty Info")]
        [SerializeField] CanvasGroup contentGroup;
        [SerializeField] VCEmptyInfo emptyInfo;
        
        public override Transform DetermineHost() => Services.Get<ViewHosting>().OnMainCanvas();
        public CanvasGroup[] ContentGroups => new[] { contentGroup };
        public VCEmptyInfo EmptyInfoView => emptyInfo;
        
        protected override void OnInitialize() {
            PrepareEmptyInfo();
        }
        
        public void PrepareEmptyInfo() {
            emptyInfo.Setup(ContentGroups);
        }
    }
}