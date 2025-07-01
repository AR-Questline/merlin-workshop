using Awaken.TG.Main.UI.Components;
using Awaken.TG.MVC;
using Awaken.TG.MVC.Attributes;
using UnityEngine;

namespace Awaken.TG.Main.UI.TitleScreen {
    [UsesPrefab("TitleScreen/VWelcomePopupUI")]
    public class VWelcomePopupUI : View<WelcomePopupUI> {
        public ARButton continueButton;
        public override Transform DetermineHost() => Services.Get<ViewHosting>().OnMainCanvas();
        
        protected override void OnInitialize() {
            continueButton.OnClick += () => Target.Discard();
        }
    }
}
