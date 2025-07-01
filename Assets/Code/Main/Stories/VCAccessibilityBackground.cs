using Awaken.TG.Main.Settings;
using Awaken.TG.Main.Settings.Accessibility;
using Awaken.TG.Main.Settings.Gameplay;
using Awaken.TG.MVC;
using UnityEngine;
using UnityEngine.UI;

namespace Awaken.TG.Main.Stories {
    public class VCAccessibilityBackground : ViewComponent {
        [SerializeField] Image accessibilityBackground;
        [SerializeField] BackgroundType backgroundType;
        
        protected override void OnAttach() {
            if (backgroundType == BackgroundType.Subtitles) {
                SubtitlesSetting setting = World.Only<SubtitlesSetting>();
                setting.ListenTo(Setting.Events.SettingChanged, () => SetBackgroundOpacity(setting), this);
                SetBackgroundOpacity(setting);
            } else {
                HudBackgroundsIntensity setting = World.Only<HudBackgroundsIntensity>();
                setting.ListenTo(Setting.Events.SettingChanged, () => SetBackgroundOpacity(setting), this);
                SetBackgroundOpacity(setting);
            }
        }
        
        void SetBackgroundOpacity(HudBackgroundsIntensity setting) {
            accessibilityBackground.color = new Color(accessibilityBackground.color.r, accessibilityBackground.color.g, accessibilityBackground.color.b, setting.Value);
        }
        
        void SetBackgroundOpacity(SubtitlesSetting setting) {
            accessibilityBackground.color = new Color(accessibilityBackground.color.r, accessibilityBackground.color.g, accessibilityBackground.color.b, setting.BackgroundIntensity);
        }
    }
    
    internal enum BackgroundType : byte {
        [UnityEngine.Scripting.Preserve] Hud = 0,
        [UnityEngine.Scripting.Preserve] Subtitles = 1        
    }
}