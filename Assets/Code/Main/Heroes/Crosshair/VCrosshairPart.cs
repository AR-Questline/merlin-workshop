using Awaken.TG.MVC;
using UnityEngine;
using UnityEngine.UI;

namespace Awaken.TG.Main.Heroes.Crosshair {
    public class VCrosshairPart<T> : View<T> where T : CrosshairPart {
        [SerializeField] Image colorableCrosshairImage;
        public Hero Hero => Target.Hero;
        public override Transform DetermineHost() => Hero.View<VHeroHUD>().crosshairParent;

        protected override void OnInitialize() {
            if (colorableCrosshairImage != null) {
                ChangeColors(Target.Crosshair.CurrentLocationType);
                Target.Crosshair.ListenTo(HeroCrosshair.Events.CrosshairLocationTypeChanged, ChangeColors, this);
            }
        }

        void ChangeColors(CrosshairTargetType type) {
            colorableCrosshairImage.color = type.Color;
        }
    }
}