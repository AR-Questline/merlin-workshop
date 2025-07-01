using System;
using System.Collections.Generic;
using Awaken.TG.Main.UI.HUD.AdvancedNotifications.LeftScreen.HeroLevelUp;
using Awaken.TG.Main.UIToolkit.PresenterData;
using Awaken.TG.MVC;
using UnityEngine.UIElements;

namespace Awaken.TG.Main.UI.HUD.AdvancedNotifications.LeftScreen.WyrdInfo {
    public partial class WyrdInfoNotificationBuffer : AdvancedNotificationBuffer<WyrdInfoNotification> {
        public sealed override bool IsNotSaved => true;

        protected override VisualElement NotificationsParent => ParentModel.NotificationsContainerUI.WyrdInfoNotificationParent;
        protected override IEnumerable<Type> DependentBuffers {
            get {
                yield return typeof(HeroLevelUpNotificationBuffer);
            }
        }
        
        protected override PBaseData RetrieveNotificationBaseData() {
            return PresenterDataProvider.wyrdInfoNotificationData.BaseData;
        }

        protected override IPAdvancedNotification<WyrdInfoNotification> MakeNotificationPresenter(VisualTreeAsset prototype) {
            PWyrdInfoNotification pWyrdInfoNotification = new(prototype.Instantiate());
            return World.BindPresenter(this, pWyrdInfoNotification);
        }
    }
}