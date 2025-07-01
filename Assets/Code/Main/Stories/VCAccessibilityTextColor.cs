using Awaken.TG.Main.Settings;
using Awaken.TG.Main.Settings.Gameplay;
using Awaken.TG.MVC;
using TMPro;
using UnityEngine;

namespace Awaken.TG.Main.Stories {
    public class VCAccessibilityTextColor : ViewComponent {
        [SerializeField] TextMeshProUGUI text;
        
        protected override void OnAttach() {
            SubtitlesSetting subsSetting = World.Only<SubtitlesSetting>();
            subsSetting.ListenTo(Setting.Events.SettingChanged, () => SetTextColor(subsSetting), this);
            SetTextColor(subsSetting);
        }
        
        void SetTextColor(SubtitlesSetting setting) {
            text.color = setting.ActiveColor;
        }
    }
}
