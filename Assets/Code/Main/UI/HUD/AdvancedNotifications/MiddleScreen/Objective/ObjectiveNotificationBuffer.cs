using System;
using System.Collections.Generic;
using Awaken.TG.Main.ActionLogs;
using Awaken.TG.Main.Localization;
using Awaken.TG.Main.Stories.Quests;
using Awaken.TG.Main.Stories.Quests.Objectives;
using Awaken.TG.Main.UI.HUD.AdvancedNotifications.Item;
using Awaken.TG.Main.UI.HUD.AdvancedNotifications.MiddleScreen.Quest;
using Awaken.TG.Main.UIToolkit.PresenterData;
using Awaken.TG.MVC;
using Awaken.TG.MVC.Events;
using Awaken.TG.Utility;
using UnityEngine.UIElements;

namespace Awaken.TG.Main.UI.HUD.AdvancedNotifications.MiddleScreen.Objective {
    public partial class ObjectiveNotificationBuffer : AdvancedNotificationBuffer<ObjectiveNotification> {
        QuestTracker _questTracker;  
        
        protected override VisualElement NotificationsParent => ParentModel.NotificationsContainerUI.ObjectiveNotificationsParent;
        protected override IEnumerable<Type> DependentBuffers {
            get {
                yield return typeof(QuestNotificationBuffer);
            }
        }
        
        protected override void OnInitialize() {
            base.OnInitialize();
            World.EventSystem.ListenTo(EventSelector.AnySource, QuestUtils.Events.ObjectiveChanged, this, ObjectiveStateChanged);
            ModelUtils.DoForFirstModelOfType<QuestTracker>(questTracker => _questTracker = questTracker, this);
        }
        
        protected override PBaseData RetrieveNotificationBaseData() {
            return PresenterDataProvider.objectiveNotificationData.BaseData;
        }

        protected override IPAdvancedNotification<ObjectiveNotification> MakeNotificationPresenter(VisualTreeAsset prototype) {
            PObjectiveNotification pObjectiveNotificationData = new(prototype.Instantiate());
            return World.BindPresenter(this, pObjectiveNotificationData);
        }

        void ObjectiveStateChanged(QuestUtils.ObjectiveStateChange stateChange) {
            bool shouldPrevent = _questTracker.ActiveQuest == stateChange.objective.ParentModel || 
                                 !stateChange.objective.ParentModel.VisibleInQuestLog ||
                                 stateChange.newState != ObjectiveState.Active ||
                                 stateChange.oldState == stateChange.newState ||
                                 World.Only<QuestNotificationBuffer>().IsQuestGoingToBeAnnounced(stateChange.objective.ParentModel);
            
            if (shouldPrevent) {
                return;
            }

            var objectiveData = new ObjectiveData(stateChange.objective);
            AdvancedNotificationBuffer.Push<ObjectiveNotificationBuffer>(new ObjectiveNotification(objectiveData));
        }
    }
}