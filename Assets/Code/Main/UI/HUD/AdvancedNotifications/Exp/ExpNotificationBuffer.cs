using Awaken.TG.Main.UIToolkit.PresenterData;
using Awaken.TG.MVC;
using UnityEngine.UIElements;

namespace Awaken.TG.Main.UI.HUD.AdvancedNotifications.Exp {
    public partial class ExpNotificationBuffer : AdvancedNotificationBufferPresenter<ExpNotification> {
        protected override VisualElement NotificationsParent => ParentModel.NotificationsContainerUI.ExpNotificationsParent;
        
        protected override PBaseData RetrieveNotificationBaseData() {
            return PresenterDataProvider.expNotificationData.BaseData;
        }

        protected override IPAdvancedNotification<ExpNotification> MakeNotificationPresenter(VisualTreeAsset prototype) {
            PExpNotification pExpNotification = new(prototype.Instantiate());
            return World.BindPresenter(this, pExpNotification);
        }

        protected override void MergeSimilarNotifications(ExpNotification expNotification) {
            float mergedExp = expNotification.gainedXP;
            while (notificationQueue.Count > 0) {
                ExpNotification queuedNotification = notificationQueue.Dequeue();
                mergedExp += queuedNotification.gainedXP;
                queuedNotification.Discard();
            }
            expNotification.OverrideGainedExp(mergedExp);
        }
    }
}