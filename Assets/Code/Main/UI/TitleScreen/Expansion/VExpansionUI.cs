using Awaken.TG.MVC;
using Awaken.TG.MVC.Attributes;
using UnityEngine;

namespace Awaken.TG.Main.UI.TitleScreen.Expansion {
    [UsesPrefab("TitleScreen/Expansion/" + nameof(VExpansionUI))]
    public class VExpansionUI : View<ExpansionUI> {
        [SerializeField] Transform contentTransform;
        [SerializeField] VSarrasExpansionEntryUI vSarrasExpansionEntryUI;
        [SerializeField] VContentExpansionEntryUI vContentExpansionEntryUI;
        
        public Transform ContentTransform => contentTransform;
        public VSarrasExpansionEntryUI VSarrasExpansionEntryUI => vSarrasExpansionEntryUI;
        public VContentExpansionEntryUI VContentExpansionEntryUI => vContentExpansionEntryUI;
        
        public override Transform DetermineHost() => Services.Get<ViewHosting>().OnMainCanvas();
    }
}