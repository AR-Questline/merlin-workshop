using System;
using System.Collections.Generic;
using Awaken.TG.Main.ActionLogs;
using Awaken.TG.Main.Localization;
using Awaken.TG.Main.Stories.Quests;
using Awaken.TG.Main.UI.HUD.AdvancedNotifications.MiddleScreen.Objective;
using Awaken.TG.Main.UIToolkit.PresenterData;
using Awaken.TG.MVC;
using Awaken.TG.MVC.Events;
using Awaken.TG.Utility;
using UnityEngine.UIElements;

namespace Awaken.TG.Main.UI.HUD.AdvancedNotifications.MiddleScreen.Quest {
    public partial class QuestNotificationBuffer : AdvancedNotificationBuffer<QuestNotification> {
        protected override VisualElement NotificationsParent => ParentModel.NotificationsContainerUI.QuestNotificationsParent;
        protected override IEnumerable<Type> DependentBuffers {
            get {
                yield return typeof(ObjectiveNotificationBuffer);
            }
        }

        protected override void OnInitialize() {
            base.OnInitialize();
            World.EventSystem.ListenTo(EventSelector.AnySource, QuestUtils.Events.QuestStateChanged, this, QuestStateChanged);
        }

        public bool IsQuestGoingToBeAnnounced(Stories.Quests.Quest quest) {
            foreach (QuestNotification questNotification in Elements<QuestNotification>()) {
                if (questNotification.questData.quest == quest) {
                    return true;
                }
            }
            
            return false;
        }

        protected override PBaseData RetrieveNotificationBaseData() {
            return PresenterDataProvider.questNotificationData.BaseData;
        }

        protected override IPAdvancedNotification<QuestNotification> MakeNotificationPresenter(VisualTreeAsset prototype) {
            PQuestNotification pQuestNotification = new(prototype.Instantiate());
            return World.BindPresenter(this, pQuestNotification);
        }

        static void QuestStateChanged(QuestUtils.QuestStateChange stateChange) {
            if (!stateChange.quest.VisibleInQuestLog || stateChange.oldState == stateChange.newState) return;

            var questData = new QuestData(stateChange);
            AdvancedNotificationBuffer.Push<QuestNotificationBuffer>(new QuestNotification(questData));
        }
    }
}