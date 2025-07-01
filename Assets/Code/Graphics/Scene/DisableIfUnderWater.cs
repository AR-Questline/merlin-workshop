using Awaken.TG.Main.Heroes;
using Awaken.TG.Main.Heroes.VolumeCheckers;
using Awaken.TG.Main.Settings.Controllers;
using Awaken.TG.MVC;

namespace Awaken.TG.Graphics.Scene {
    public class DisableIfUnderWater : StartDependentView<Hero> {
        protected override void OnInitialize() {
            Target.ListenTo(VCHeroWaterChecker.Events.WaterCollisionStateChanged, OnWaterCollisionStateChanged, this);
            OnWaterCollisionStateChanged(Target.IsUnderWater);
        }
        
        void OnWaterCollisionStateChanged(bool inWater) {
            gameObject.SetActive(!inWater);
        }
    }
}