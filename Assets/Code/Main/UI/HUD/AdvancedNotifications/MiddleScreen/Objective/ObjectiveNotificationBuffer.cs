using System;
using System.Collections.Generic;
using Awaken.TG.Main.Locations.Shops.UI;
using Awaken.TG.Main.Stories;
using Awaken.TG.Main.Stories.Quests;
using Awaken.TG.Main.Stories.Quests.Objectives;
using Awaken.TG.Main.UI.HUD.AdvancedNotifications.MiddleScreen.Quest;
using Awaken.TG.Main.UIToolkit.PresenterData;
using Awaken.TG.MVC;
using Awaken.TG.MVC.Events;
using UnityEngine.UIElements;

namespace Awaken.TG.Main.UI.HUD.AdvancedNotifications.MiddleScreen.Objective {
    public partial class ObjectiveNotificationBuffer : AdvancedNotificationBufferPresenter<ObjectiveNotification> {
        QuestTracker _questTracker;  
        
        protected override bool HideWhenMapNotInteractive => !World.HasAny<Story>() || World.HasAny<ShopUI>();
        protected override VisualElement NotificationsParent => ParentModel.NotificationsContainerUI.ObjectiveNotificationsParent;
        protected override IEnumerable<Type> DependentBuffers {
            get {
                yield return typeof(QuestNotificationBuffer);
            }
        }
        
        protected override void OnInitialize() {
            base.OnInitialize();
            ModelUtils.DoForFirstModelOfType<QuestTracker>(Init, this);
        }
        
        protected override PBaseData RetrieveNotificationBaseData() {
            return PresenterDataProvider.objectiveNotificationData.BaseData;
        }

        protected override IPAdvancedNotification<ObjectiveNotification> MakeNotificationPresenter(VisualTreeAsset prototype) {
            PObjectiveNotification pObjectiveNotificationData = new(prototype.Instantiate());
            return World.BindPresenter(this, pObjectiveNotificationData);
        }

        void Init(QuestTracker questTracker) {
            _questTracker = questTracker;
            World.EventSystem.ListenTo(EventSelector.AnySource, QuestUtils.Events.ObjectiveChanged, this, ObjectiveStateChanged);
        }

        void ObjectiveStateChanged(QuestUtils.ObjectiveStateChange stateChange) {
            bool shouldPrevent = _questTracker.ActiveQuest == stateChange.objective.ParentModel || 
                                 !stateChange.objective.ParentModel.VisibleInQuestLog ||
                                 (stateChange.newState != ObjectiveState.Active && !World.Any<Story>()) ||
                                 stateChange.oldState == stateChange.newState ||
                                 World.Only<QuestNotificationBuffer>().IsQuestGoingToBeAnnounced(stateChange.objective.ParentModel);
            
            if (shouldPrevent) {
                return;
            }

            var objectiveData = new ObjectiveData(stateChange.objective);
            NotificationUtils.Push(new ObjectiveNotification(objectiveData));
        }
    }
}