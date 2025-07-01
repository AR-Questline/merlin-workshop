using Awaken.TG.MVC;
using Awaken.TG.MVC.Attributes;
using Awaken.TG.Utility;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Awaken.TG.Main.UI.HUD.Notifications {
    [UsesPrefab("HUD/Notifications/VNotificationItem")]
    public class VNotificationItem : View<NotificationItem> {
        public override Transform DetermineHost() => Target.ParentModel.View<VNotification>().transform;

        // === References
        public Image icon;
        public TextMeshProUGUI message;

        // === Initialization
        protected override void OnInitialize() {
            Target.ListenTo(Model.Events.AfterChanged, Refresh, this);
            Refresh();
        }

        void Refresh() {
            bool useIconRef = (Target.iconRef?.arSpriteReference.IsSet ?? false) && string.IsNullOrWhiteSpace(Target.iconString);
            
            if (useIconRef) {
                icon.gameObject.SetActive(true);
                Target.iconRef.RegisterAndSetup(this, icon);
            } else {
                icon.gameObject.SetActive(false);
            }

            bool hasIconStrong = !string.IsNullOrWhiteSpace(Target.iconString);
            string iconMsg = useIconRef || !hasIconStrong ? "" : $"{Target.iconString.FormatSprite()} ";
            message.text = Target.amount != null ? $"{iconMsg}{Target.amount.Value} {Target.message}" : message.text = $"{iconMsg}{Target.message}";
        }
    }
}