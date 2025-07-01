using Awaken.TG.Assets;
using Awaken.TG.Main.Localization;
using Awaken.TG.Main.Locations.Attachments;
using Awaken.TG.Main.Scenes;
using Awaken.TG.MVC;
using Awaken.TG.MVC.Domains;
using Awaken.TG.MVC.Events;
using JetBrains.Annotations;

namespace Awaken.TG.Main.Stories.Quests.Objectives.Trackers {
    public abstract partial class BaseSceneSpecificTracker : BaseTracker {
        // === State
        protected LocString ChangeSceneDescription { get; set; }
        protected SceneReference TargetScene { get; set; }

        // === Properties
        public override string DescriptionOverride => !OnCorrectScene
            ? ChangeSceneDescription
                .Translate()
                .Replace("{sceneName}", LocTerms.GetSceneName(TargetScene))
            : null;
        public override bool IsCompleted => OnCorrectScene && TrackerCompleted;
        protected abstract bool TrackerCompleted { get; }
        protected bool OnCorrectScene { get; private set; }
        
        // === Initialization
        protected override void OnInitialize() {
            CheckScene(World.Services.Get<SceneService>().ActiveSceneRef);
        }

        protected override void OnRestore() {
            
        }
        
        protected override void OnFullyInitialized() {
            World.EventSystem.ListenTo(EventSelector.AnySource, SceneLifetimeEvents.Events.SafeAfterSceneChanged, this, CheckScene);
            base.OnFullyInitialized();
        }

        // === Operations
        void CheckScene(SceneLifetimeEvents _) => CheckScene(World.Services.Get<SceneService>().ActiveSceneRef);
        
        void CheckScene(SceneReference sceneReference) {
            var onCorrectScene = sceneReference == TargetScene;
            if (onCorrectScene != OnCorrectScene) {
                OnCorrectScene = onCorrectScene;
                if (OnCorrectScene) {
                    OnCorrectSceneEntered();
                } else {
                    OnCorrectSceneLeft();
                }
                CheckIfCompleted();
                TriggerChange();
            }
        }

        protected abstract void OnCorrectSceneEntered();
        protected abstract void OnCorrectSceneLeft();

        // === Helpers
        protected override string ConstructDesc() {
            return OnSceneDesc();
        }

        protected virtual string OnSceneDesc() {
            string baseDesc = CustomDisplayPattern.toggled ? CustomDisplayPattern.LocString : DisplayPattern.LocalizedPattern;
            if (StartDisplayPatternInNewLine) {
                baseDesc = $"\n{baseDesc}";
            }
            return baseDesc;
        }
    }
    
    public abstract partial class BaseSceneSpecificTracker<T> : BaseSceneSpecificTracker, IRefreshedByAttachment<T> where T : BaseSceneSpecificTrackerAttachment {
        // === Constructors
        public virtual void InitFromAttachment(T spec, bool isRestored) {
            DisplayPattern = spec.DisplayPattern ?? TrackerDisplayPattern.None;
            CustomDisplayPattern = spec.customDisplayPattern;
            StartDisplayPatternInNewLine = spec.startDisplayPatternInNewLine;
            ChangeSceneDescription = spec.changeSceneDescription;
            TargetScene = spec.targetScene;
            FlagToMap = spec.flagToMap;
            WhenOtherTrackersPresent = spec.whenOtherTrackersPresent;
            TaskType = spec.taskType;
            RequiredObjectiveState = spec.requiredObjectiveState;
            TargetState = spec.targetState;
            ObjectiveChangesOnFulfilled = spec.objectiveChangesOnFulfilled;
            ObjectiveChangesOnLoseFulfillment = spec.objectiveChangesOnLoseFulfillment;
        }
    }
}