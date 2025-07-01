using System;
using System.Collections.Generic;
using Awaken.TG.Main.UI.HUD.AdvancedNotifications.LeftScreen.HeroLevelUp;
using Awaken.TG.Main.UI.HUD.AdvancedNotifications.LeftScreen.LocationDiscovery;
using Awaken.TG.Main.UI.HUD.AdvancedNotifications.LeftScreen.WyrdInfo;
using Awaken.TG.Main.UIToolkit.PresenterData;
using Awaken.TG.MVC;
using Awaken.Utility;
using UnityEngine.UIElements;

namespace Awaken.TG.Main.UI.HUD.AdvancedNotifications.LeftScreen.Journal {
    public partial class JournalUnlockNotificationBuffer : AdvancedNotificationBuffer<JournalUnlockNotification> {
        public sealed override bool IsNotSaved => true;

        public override bool SuspendPushingNotifications => PlatformUtils.IsJournalDisabled || base.SuspendPushingNotifications;

        protected override VisualElement NotificationsParent => ParentModel.NotificationsContainerUI.JournalUnlockNotificationParent;
        protected override IEnumerable<Type> DependentBuffers {
            get {
                yield return typeof(LocationDiscoveryBuffer);
            }
        }

        protected override PBaseData RetrieveNotificationBaseData() {
            return PresenterDataProvider.journalUnlockNotificationData.BaseData;
        }

        protected override IPAdvancedNotification<JournalUnlockNotification> MakeNotificationPresenter(VisualTreeAsset prototype) {
            PJournalUnlockNotification notification = new(prototype.Instantiate());
            return World.BindPresenter(this, notification);
        }
    }
}