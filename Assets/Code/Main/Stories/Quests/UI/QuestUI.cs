using System;
using Awaken.TG.Main.General.NewThings;
using Awaken.TG.MVC;
using Awaken.TG.MVC.Attributes;
using Awaken.TG.MVC.Elements;

namespace Awaken.TG.Main.Stories.Quests.UI {
    [SpawnsView(typeof(VQuestUI))]
    public partial class QuestUI : Element<QuestLogUI>, INewThingCarrier {
        public sealed override bool IsNotSaved => true;

        public Quest QuestData { get; }

        public bool IsVisible => View<VQuestUI>().IsVisible;
        public bool IsTracked => World.Only<QuestTracker>().ActiveQuest == QuestData;
        public bool IsSelected { get; private set; }
        
        public IModelNewThing NewThingModel => QuestData;
        public event Action onNewThingRefresh;
        
        public QuestUI(Quest quest) {
            QuestData = quest;
        }
        
        public bool IsQuestCompleted => QuestData.State == QuestState.Completed;
        public bool IsQuestFailed => QuestData.State == QuestState.Failed;
        public bool IsQuestFinished => IsQuestCompleted || IsQuestFailed;
        public QuestType QuestType => QuestData.Template.TypeOfQuest;

        void Unselect() {
            IsSelected = false;
            TriggerChange();
        }

        public void Select() {
            if (IsSelected) {
                if (QuestData.State != QuestState.Completed && QuestData.State != QuestState.Failed) {
                    ParentModel.SetTrackedQuest(QuestData);
                }
            } else {
                ParentModel.SelectedQuest?.Unselect();
                DescriptionDisplayed();
                IsSelected = true;
                
                ((INewThingCarrier)this).MarkSeen();
                onNewThingRefresh?.Invoke();
            }
            ParentModel.Trigger(QuestLogUI.Events.QuestSelected, this);
            TriggerChange();
        }

        public void ChangeVisibility(bool visible) {
            View<VQuestUI>().ChangeVisibility(visible);
        }
        
        void DescriptionDisplayed() {
            QuestData.DisplayedByPlayer = true;
        }
    }
}
