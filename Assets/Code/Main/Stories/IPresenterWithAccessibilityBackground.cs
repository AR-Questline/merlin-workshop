using Awaken.TG.Main.Settings;
using Awaken.TG.Main.Settings.Accessibility;
using Awaken.TG.MVC;
using UnityEngine.UIElements;

namespace Awaken.TG.Main.Stories {
    public interface IPresenterWithAccessibilityBackground { 
        VisualElement Host { get; }
        string AdjustedBackgroundClass => "adjustable-background";

        void InitializeBackground(IModel owner) {
            World.Only<HudBackgroundsIntensity>().ListenTo(Setting.Events.SettingChanged, SetBackground, owner);
            SetBackground();
        }
        
        void SetBackground() {
            var setting = World.Only<HudBackgroundsIntensity>();
            
            if (setting.AreHudBackgroundsAllowed) {
                Host.AddToClassList(AdjustedBackgroundClass);
                var newColor = Host.style.backgroundColor.value;
                newColor.a = setting.Value;
                Host.style.unityBackgroundImageTintColor = newColor;
            } else {
                Host.RemoveFromClassList(AdjustedBackgroundClass);
            }
        }
    }
}
