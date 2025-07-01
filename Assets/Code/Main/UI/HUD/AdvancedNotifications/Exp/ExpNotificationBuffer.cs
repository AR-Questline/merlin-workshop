using Awaken.TG.Main.UIToolkit.PresenterData;
using Awaken.TG.MVC;
using UnityEngine.UIElements;

namespace Awaken.TG.Main.UI.HUD.AdvancedNotifications.Exp {
    public partial class ExpNotificationBuffer : AdvancedNotificationBuffer<ExpNotification> {
        public sealed override bool IsNotSaved => true;

        protected override VisualElement NotificationsParent => ParentModel.NotificationsContainerUI.ExpNotificationsParent;
        
        protected override PBaseData RetrieveNotificationBaseData() {
            return PresenterDataProvider.expNotificationData.BaseData;
        }

        protected override IPAdvancedNotification<ExpNotification> MakeNotificationPresenter(VisualTreeAsset prototype) {
            PExpNotification pExpNotification = new(prototype.Instantiate());
            return World.BindPresenter(this, pExpNotification);
        }
    }
}