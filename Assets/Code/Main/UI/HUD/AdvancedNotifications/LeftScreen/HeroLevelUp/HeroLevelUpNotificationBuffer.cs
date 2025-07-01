using System;
using System.Collections.Generic;
using Awaken.TG.Main.UI.HUD.AdvancedNotifications.LeftScreen.WyrdInfo;
using Awaken.TG.Main.UIToolkit.PresenterData;
using Awaken.TG.MVC;
using UnityEngine.UIElements;

namespace Awaken.TG.Main.UI.HUD.AdvancedNotifications.LeftScreen.HeroLevelUp {
    public partial class HeroLevelUpNotificationBuffer : AdvancedNotificationBuffer<HeroLevelUpNotification> {
        public sealed override bool IsNotSaved => true;

        protected override VisualElement NotificationsParent => ParentModel.NotificationsContainerUI.LevelUpNotificationsParent;
        protected override IEnumerable<Type> DependentBuffers {
            get {
                yield return typeof(WyrdInfoNotificationBuffer);
            }
        }
        
        protected override PBaseData RetrieveNotificationBaseData() {
            return PresenterDataProvider.levelUpNotificationData.BaseData;
        }

        protected override IPAdvancedNotification<HeroLevelUpNotification> MakeNotificationPresenter(VisualTreeAsset prototype) {
            PHeroLevelUpNotification pHeroLevelUpNotification = new(prototype.Instantiate());
            return World.BindPresenter(this, pHeroLevelUpNotification);
        }
    }
}