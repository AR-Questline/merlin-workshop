using Awaken.TG.MVC.Attributes;
using TMPro;
using UnityEngine;

namespace Awaken.TG.Main.UI.HUD.AdvancedNotifications.MiddleScreen.FancyPanel {
    [UsesPrefab("HUD/AdvancedNotifications/VFancyPanelNotification")]
    public class VFancyPanelNotification : VAdvancedNotification<FancyPanelNotification> {
        [SerializeField] TextMeshProUGUI mainText;
        
        protected override void OnInitialize() {
            mainText.text = Target.text;
        }
    }
}