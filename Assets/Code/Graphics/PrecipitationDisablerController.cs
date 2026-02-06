using Awaken.TG.Main.Settings.Controllers;

namespace Awaken.TG.Graphics {
    public class PrecipitationDisablerController : StartDependentView<WeatherController> {
        protected override void OnMount() {
            base.OnMount();
            Target.SnowBlendIn.SetInstant(0f);
            Target.RainBlendIn.SetInstant(0f);
        }
    }
}