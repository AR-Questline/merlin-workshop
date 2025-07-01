using Awaken.TG.MVC.Attributes;
using TMPro;
using UnityEngine;

namespace Awaken.TG.Main.UI.HUD.AdvancedNotifications.MiddleScreen.FancyPanel {
    [UsesPrefab("HUD/AdvancedNotifications/VLowerFancyPanelNotification")]
    public class VLowerFancyPanelNotification : VAdvancedNotification<LowerFancyPanelNotification> {
        [SerializeField] TextMeshProUGUI mainText;
        public override Transform DetermineHost() => Target.GenericParentModel.View<IViewNotificationBuffer>().NotificationParent;
        
        protected override void OnInitialize() {
            mainText.text = Target.text;
        }
    }
}