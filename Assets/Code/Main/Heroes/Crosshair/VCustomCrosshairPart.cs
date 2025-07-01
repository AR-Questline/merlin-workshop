using Awaken.TG.MVC.Attributes;
using UnityEngine.UI;

namespace Awaken.TG.Main.Heroes.Crosshair {
    [UsesPrefab("UI/Crosshair/VCustomCrosshairPart")]
    public class VCustomCrosshairPart : VCrosshairPart<CustomCrosshairPart> {
        public Image image;

        protected override void OnInitialize() {
            Target.SpriteReference.RegisterAndSetup(this, image);
        }
    }
}