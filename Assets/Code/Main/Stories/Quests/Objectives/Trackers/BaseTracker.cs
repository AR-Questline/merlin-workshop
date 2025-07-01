using System.Collections.Generic;
using Awaken.TG.Main.Localization;
using Awaken.TG.Main.Memories;
using Awaken.TG.Main.Scenes;
using Awaken.TG.Main.Stories.Quests.Objectives.Specs;
using Awaken.TG.MVC;
using Awaken.TG.MVC.Elements;
using Awaken.TG.Utility;
using Awaken.TG.Utility.Attributes;
using Awaken.Utility.Extensions;

namespace Awaken.TG.Main.Stories.Quests.Objectives.Trackers {
    public abstract partial class BaseTracker : Element<Objective> {
        // === State
        [Saved] public float Current { get; protected set; }
        
        public OtherTrackersBehaviour WhenOtherTrackersPresent { get; protected set; }
        public TaskType TaskType { get; protected set; }
        public ObjectiveStateFlag RequiredObjectiveState { get; protected set; }
        public TaskState TargetState { get; protected set; }
        public List<ObjectiveChange> ObjectiveChangesOnFulfilled { get; protected set; }
        public List<ObjectiveChange> ObjectiveChangesOnLoseFulfillment { get; protected set; }
        public TrackerDisplayPattern DisplayPattern { get; set; }
        protected OptionalLocString CustomDisplayPattern { get; set; }
        protected string FlagToMap { get; set; }
        public bool StartDisplayPatternInNewLine { get; set; }

        bool _wasCompleted;
        
        // === Properties
        public string TrackingDesc() => GetFinalDesc();
        public virtual string DescriptionOverride => null;
        public abstract bool IsCompleted { get; }
        protected virtual bool StrikeThroughCompleted => true;
        public bool CanBeCompleted => RequiredObjectiveState.HasThisState(ParentModel.State);

        Quest Quest => ParentModel.ParentModel;
        
        // === Initialization
        protected override void OnFullyInitialized() {
            if (RequiredObjectiveState != ObjectiveStateFlag.All) {
                Quest.ListenTo(QuestUtils.Events.ObjectiveChanged, OnObjectiveChange, this);
            }
            if (SceneLifetimeEvents.Get.EverythingInitialized) {
                CheckIfCompletedInitial();
            } else {
                World.EventSystem.LimitedListenTo(SceneLifetimeEvents.Get.ID, SceneLifetimeEvents.Events.SafeAfterSceneChanged, this, _ => CheckIfCompletedInitial(), 1);
            }
        }

        // === Operations

        void OnObjectiveChange(QuestUtils.ObjectiveStateChange change) {
            if (change.objective != ParentModel) {
                return;
            }
            if (change.oldState == change.newState) {
                return;
            }
            
            if (RequiredObjectiveState.HasThisState(change.newState)) {
                if (_wasCompleted && ParentModel.CanBeCompletedMultipleTimes && !RequiredObjectiveState.HasThisState(change.oldState)) {
                    _wasCompleted = false;
                }
                CheckIfCompletedInternal(true);
            }
        }

        void CheckIfCompletedInitial() {
            if (RequiredObjectiveState.HasThisState(ParentModel.State)) {
                CheckIfCompletedInternal(false);
            }
        }
        
        protected void CheckIfCompleted() {
            if (RequiredObjectiveState.HasThisState(ParentModel.State)) {
                CheckIfCompletedInternal(true);
            }
        }

        void CheckIfCompletedInternal(bool triggerChangeEvenIfNotCompleted) {
            if (CanBeCompleted && IsCompleted && !_wasCompleted) {
                ParentModel.OnTrackerFulfilled(this);
                _wasCompleted = true;
                TryMapStateToFlag();
            } else if (_wasCompleted && !IsCompleted) {
                ParentModel.OnTrackerFulfillmentLost(this);
                _wasCompleted = false;
                TryMapStateToFlag();
            } else if (triggerChangeEvenIfNotCompleted) {
                ObjectiveState state = ParentModel.State;
                QuestUtils.ObjectiveStateChange stateChange = new() { objective = ParentModel, oldState = state, newState = state };
                Quest.Trigger(QuestUtils.Events.ObjectiveChanged, stateChange);
            }
        }

        void TryMapStateToFlag() {
            if (!FlagToMap.IsNullOrWhitespace()) {
                Services.Get<GameplayMemory>().Context().Set(FlagToMap, _wasCompleted);
            }
        }

        // === Helpers
        protected virtual string ConstructDesc() => string.Empty;
        
        string GetFinalDesc() {
            var desc = ConstructDesc();
            return StrikeThroughCompleted && IsCompleted ? desc.StrikeThrough() : desc;
        }
    }
}