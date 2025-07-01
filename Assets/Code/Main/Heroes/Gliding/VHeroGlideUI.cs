using Awaken.TG.MVC;
using Awaken.TG.MVC.Attributes;
using UnityEngine;

namespace Awaken.TG.Main.Heroes.Gliding {
    [UsesPrefab("HUD/VHeroGlideUI")]
    public class VHeroGlideUI : View<HeroGlideUI> {
        public override Transform DetermineHost() => Services.Get<ViewHosting>().OnAlwaysVisibleHUD();
    }
}
