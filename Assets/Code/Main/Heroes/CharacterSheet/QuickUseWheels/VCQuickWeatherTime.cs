using Awaken.TG.MVC;
using TMPro;
using UnityEngine;

namespace Awaken.TG.Main.Heroes.CharacterSheet.QuickUseWheels {
    public class VCQuickWeatherTime : ViewComponent<QuickUseWheelUI> {
        [SerializeField] TextMeshProUGUI gameWeatherTimeText;
        [SerializeField] Transform arm;

        protected override void OnAttach() {
            int hour = Target.WeatherTime.Hour;
            int minute = Target.WeatherTime.Minutes;
            gameWeatherTimeText.SetText($"{hour:00}:{minute:00}");
            
            float hourFraction = hour + (minute / 60f);
            float angle = (hourFraction / 24f) * 360f;
            arm.localRotation = Quaternion.Euler(0, 0, -angle);
        }
    }
}