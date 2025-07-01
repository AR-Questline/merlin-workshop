using Awaken.TG.MVC;
using Awaken.TG.MVC.Attributes;
using TMPro;
using UnityEngine;

namespace Awaken.TG.Main.UI {
    [UsesPrefab("HUD/VWaterMark")]
    public class VWaterMark : View<Model> {
        public override Transform DetermineHost() => Services.Get<ViewHosting>().OnMainCanvas();

        protected override void OnInitialize() {
            Invoke(nameof(ChangeTextAlpha), 30f);
        }

        void ChangeTextAlpha() {
            foreach (var tmpugui in GetComponentsInChildren<TextMeshProUGUI>()) {
                Color c = tmpugui.color;
                tmpugui.color = new Color(c.r,c.g,c.b, 0.05f);
            }
        }
    }
}
