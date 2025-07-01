using TMPro;

namespace Awaken.TG.MVC.UI.Handlers.Tooltips {
    public class VCTooltipText : VCTooltipElement {
        public TextMeshProUGUI text;
        
        public override void UpdateContent(object value) {
            text.text = value as string;
        }
    }
}