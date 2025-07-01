using System;
using System.Collections.Generic;
using Awaken.TG.Main.UI.HUD.AdvancedNotifications.LeftScreen.Journal;
using Awaken.TG.Main.UIToolkit.PresenterData;
using Awaken.TG.MVC;
using UnityEngine.UIElements;

namespace Awaken.TG.Main.UI.HUD.AdvancedNotifications.LeftScreen.LocationDiscovery {
    public partial class LocationDiscoveryBuffer : AdvancedNotificationBuffer<NewLocationNotification> {
        public sealed override bool IsNotSaved => true;

        protected override VisualElement NotificationsParent => ParentModel.NotificationsContainerUI.LocationNotificationsParent;
        protected override IEnumerable<Type> DependentBuffers {
            get {
                yield return typeof(JournalUnlockNotificationBuffer);
            }
        }
        
        protected override PBaseData RetrieveNotificationBaseData() {
            return PresenterDataProvider.locationNotificationData.BaseData;
        }

        protected override IPAdvancedNotification<NewLocationNotification> MakeNotificationPresenter(VisualTreeAsset prototype) {
            PLocationNotification pLocationNotification = new(prototype.Instantiate());
            return World.BindPresenter(this, pLocationNotification);
        }
    }
}