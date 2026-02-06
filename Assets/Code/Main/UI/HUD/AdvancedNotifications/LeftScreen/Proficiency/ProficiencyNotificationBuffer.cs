using System;
using System.Collections.Generic;
using Awaken.TG.Main.UI.HUD.AdvancedNotifications.LeftScreen.SpecialItem;
using Awaken.TG.Main.UIToolkit.PresenterData;
using Awaken.TG.MVC;
using UnityEngine.UIElements;

namespace Awaken.TG.Main.UI.HUD.AdvancedNotifications.LeftScreen.Proficiency {
    public partial class ProficiencyNotificationBuffer : AdvancedNotificationBufferPresenter<ProficiencyNotification> {
        protected override VisualElement NotificationsParent => ParentModel.NotificationsContainerUI.ProficiencyNotificationsParent;
        protected override IEnumerable<Type> DependentBuffers {
            get {
                yield return typeof(SpecialItemNotificationBuffer);
            }
        }

        protected override void MergeSimilarNotifications(ProficiencyNotification proficiencyNotification) {
            int maxLevelInQueue = proficiencyNotification.proficiencyData.newSkillLevel;
            while (notificationQueue.Count > 0) {
                ProficiencyNotification queuedNotification = notificationQueue.Dequeue();
                maxLevelInQueue = Math.Max(maxLevelInQueue, queuedNotification.proficiencyData.newSkillLevel);
                queuedNotification.Discard();
            }
            proficiencyNotification.OverrideProficiencyLevel(maxLevelInQueue);
        }

        protected override PBaseData RetrieveNotificationBaseData() {
            return PresenterDataProvider.proficiencyNotificationData.BaseData;
        }

        protected override IPAdvancedNotification<ProficiencyNotification> MakeNotificationPresenter(VisualTreeAsset prototype) {
            PProficiencyNotification pProficiencyNotification = new(prototype.Instantiate());
            return World.BindPresenter(this, pProficiencyNotification);
        }
    }
}