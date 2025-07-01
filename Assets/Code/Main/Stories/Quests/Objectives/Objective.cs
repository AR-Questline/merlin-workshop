using System;
using System.Collections.Generic;
using Awaken.TG.Assets;
using Awaken.TG.Main.Fights.NPCs;
using Awaken.TG.Main.Fights.NPCs.Presences;
using Awaken.TG.Main.Fights.Utils;
using Awaken.TG.Main.General.Caches;
using Awaken.TG.Main.Grounds;
using Awaken.TG.Main.Heroes;
using Awaken.TG.Main.Heroes.CharacterSheet.Map.Markers;
using Awaken.TG.Main.Localization;
using Awaken.TG.Main.Locations;
using Awaken.TG.Main.Locations.Attachments;
using Awaken.TG.Main.Locations.Attachments.Elements;
using Awaken.TG.Main.Maps.Markers;
using Awaken.TG.Main.Memories;
using Awaken.TG.Main.Scenes;
using Awaken.TG.Main.Stories.Quests.Objectives.Effectors;
using Awaken.TG.Main.Stories.Quests.Objectives.Specs;
using Awaken.TG.Main.Stories.Quests.Objectives.Trackers;
using Awaken.TG.Main.UI.TitleScreen.Loading;
using Awaken.TG.Main.Utility.Tags;
using Awaken.TG.MVC;
using Awaken.TG.MVC.Domains;
using Awaken.TG.MVC.Elements;
using Awaken.TG.MVC.Events;
using Awaken.TG.MVC.Utils;
using Awaken.TG.Utility;
using Awaken.TG.Utility.Attributes;
using Awaken.Utility;
using Awaken.Utility.Collections;
using Awaken.Utility.Extensions;
using Cysharp.Threading.Tasks;
using JetBrains.Annotations;

namespace Awaken.TG.Main.Stories.Quests.Objectives {
    public partial class Objective : Element<Quest>, IRefreshedByAttachment<ObjectiveSpecBase> {
        public override ushort TypeForSerialization => SavedModels.Objective;

        // === State

        /// <summary>
        /// Identifier
        /// </summary>
        [Saved] public string Guid { get; private set; }
        
        /// <summary>
        /// Name should be used only for analytics
        /// </summary>
        [Saved] public string Name { get; private set; }
        
        /// <summary>
        /// Description is displayed in QuestLog
        /// </summary>
        public LocString Description { get; private set; }
        
        /// <summary>
        /// Short description is preferred over standard Description in Quest Tracker
        /// </summary>
        public OptionalLocString TrackerDescription { get; private set; }

        public bool CanBeCompletedMultipleTimes { get; private set; }
        
        public int TargetLevel { get; private set; }

        public float ExperiencePoints { get; private set; }

        public MarkerData[] MarkersData { get; private set; }
        public SceneReference MainTargetOpenWorldScene => MarkersData.Length > 0 ? MarkersData[0].OpenWorldScene : null;
        public SceneReference MainTargetScene => MarkersData.Length > 0 ? MarkersData[0].TargetScene : null;

        public bool AnyMarkerVisible {
            get {
                for (int i = 0; i < MarkersData.Length; i++) {
                    if (MarkersData[i].MarkerVisible) {
                        return true;
                    }
                }
                return false;
            }
        }

        public IEnumerable<ObjectiveChange> AutoRunAfterCompletion { get; private set; }
        public IEnumerable<ObjectiveChange> AutoRunAfterFailure { get; private set; }

        [Saved] bool WasCounted { get; set; }
        
        [Saved] AttachmentTracker _tracker;
        
        ObjectiveSpecBase _specForInit;
        // === Properties
        public ObjectiveState State => Services.Get<GameplayMemory>().Context(this).Get("state", ObjectiveState.Inactive);
        public ModelsSet<BaseTracker> Trackers => Elements<BaseTracker>();
        
        bool IsAchievement => !ParentModel.CanBeTracked;

        public override string ContextID => QuestUtils.ContextID(this);

        // === Initialization
        protected override void OnAfterDeserialize() {
            _tracker.SetOwner(this);
        }
        
        public void InitFromAttachment(ObjectiveSpecBase spec, bool isRestored) {
            _specForInit = spec;
            Description = spec.Description;
            TrackerDescription = spec.TrackerDescription;
            CanBeCompletedMultipleTimes = spec.CanBeCompletedMultipleTimes;
            TargetLevel = spec.TargetLevel;
            MarkersData = spec.GetMarkersData();
            AutoRunAfterCompletion = spec.AutoRunAfterCompletion;
            AutoRunAfterFailure = spec.AutoRunAfterFailure;
        }

        protected override void OnPreRestore() {
            _tracker.PreRestore(_specForInit.Yield());
        }

        protected override void OnInitialize() {
            Name = _specForInit.GetName();
            Guid = _specForInit.Guid;
            Init();
            _tracker = new AttachmentTracker();
            _tracker.SetOwner(this);
            _tracker.Initialize(_specForInit.Yield());
            _specForInit = null;
        }
        
        protected override void OnRestore() {
            Init();
            _specForInit = null;
        }

        void Init() {
            if (State == ObjectiveState.Active) {
                TryAttachPresenceTracker(false);
            }
            ExperiencePoints = QuestUtils.CalculateXp(TargetLevel, _specForInit.ExperienceGainRange, _specForInit.ExperiencePoints);
            ParentModel.ListenTo(QuestUtils.Events.ObjectiveChanged, OnObjectiveChange, this);
            
            World.EventSystem.ListenTo(EventSelector.AnySource, SceneLifetimeEvents.Events.SafeAfterSceneChanged, this, OnSceneChanged);

            if (IsAchievement) {
                WasCounted = true;
            }
            TryToIncrementQuestCounter();
            for (int i = 0; i < MarkersData.Length; i++) {
                if (MarkersData[i].IsMarkerRelatedToStory) {
                    World.EventSystem.ListenTo(EventSelector.AnySource, StoryFlags.Events.UniqueFlagChanged(MarkersData[i].RelatedStoryFlag.Flag), this, OnRelatedStoryFlagChanged);
                }
            }
        }

        protected override void OnFullyInitialized() {
            TriggerEffectors(new QuestUtils.ObjectiveStateChange {objective = this, oldState = State, newState = State});
            UpdateMarkersAfterLoad().Forget();
        }
        
        // === Trackers
        
        async UniTask UpdateMarkersAfterLoad() {
            var loading = World.Any<LoadingScreenUI>();
            if (loading) {
                await AsyncUtil.WaitForDiscard(loading);
                if (State == ObjectiveState.Active) {
                    TryAttachPresenceTracker();
                }
            }
            
            UpdateMapMarkers();
        }
        
        public void OnTrackerFulfilled(BaseTracker tracker) {
            if (tracker.WhenOtherTrackersPresent == OtherTrackersBehaviour.Independent) {
                TrackerCompleted(tracker);
            } else if (tracker.WhenOtherTrackersPresent == OtherTrackersBehaviour.RequireAll) {
                var allCompleted = true;
                foreach (var t in Trackers) {
                    if (t.WhenOtherTrackersPresent == OtherTrackersBehaviour.RequireAll && !t.IsCompleted) {
                        allCompleted = false;
                        break;
                    }
                }
                if (allCompleted) {
                    TrackerCompleted(tracker);
                }
            }
        }
        
        public void OnTrackerFulfillmentLost(BaseTracker tracker) {
            if (tracker.TaskType == TaskType.OtherObjectives) {
                QuestUtils.AutoRunTrackerFulfillmentLostObjectives(ParentModel, tracker);
            }
        }

        public void TrackingChanged() {
            UpdateMapMarkers();
        }
        
        void TrackerCompleted(BaseTracker tracker) {
            switch (tracker.TaskType) {
                case TaskType.Objective when tracker.TargetState == TaskState.Completed:
                    QuestUtils.ChangeObjectiveState(this, ObjectiveState.Completed);
                    break;
                case TaskType.Objective when tracker.TargetState == TaskState.Failed:
                    QuestUtils.ChangeObjectiveState(this, ObjectiveState.Failed);
                    break;
                case TaskType.Quest when tracker.TargetState == TaskState.Completed:
                    QuestUtils.SetQuestState(ParentModel, QuestState.Completed);
                    break;
                case TaskType.Quest when tracker.TargetState == TaskState.Failed:
                    QuestUtils.SetQuestState(ParentModel, QuestState.Failed);
                    break;
                case TaskType.OtherObjectives:
                    QuestUtils.AutoRunTrackerFulfillmentObjectives(ParentModel, tracker);
                    break;
            }
        }

        // === Callbacks
        void OnRelatedStoryFlagChanged() {
            UpdateMapMarkers();
            this.Trigger(QuestUtils.Events.ObjectiveRelatedStoryFlagChanged, true);
        }
        
        void OnObjectiveChange(QuestUtils.ObjectiveStateChange stateChange) {
            if (stateChange.objective != this) return;
            ObjectiveState state = stateChange.newState;
            
            if (state == stateChange.oldState) {
                return;
            }
            
            if (state == ObjectiveState.Active) {
                TryAttachPresenceTracker();
            } else {
                TryDetachPresenceTracker();
            }
            
            TriggerEffectors(stateChange);
            
            if (state is ObjectiveState.Completed or ObjectiveState.Failed) {
                if (!CanBeCompletedMultipleTimes || state is not ObjectiveState.Completed) {
                    RemoveElementsOfType<BaseTracker>();
                }
                TryToIncrementQuestCounter();
            }
            
            UpdateMapMarkers();
        }

        void TryToIncrementQuestCounter() {
            if (!WasCounted && State == ObjectiveState.Completed) {
                WasCounted = true;
                this.Trigger(QuestUtils.Events.ObjectiveCompleted, this);
            }
        }

        void TriggerEffectors(QuestUtils.ObjectiveStateChange stateChange) {
            foreach (var effector in Elements<IObjectiveEffector>().ToArraySlow()) {
                effector.OnStateUpdate(stateChange);
            }
        }

        void OnSceneChanged(SceneLifetimeEvents _) {
            if (!IsFullyInitialized) return;
            UpdateMapMarkers();
        }

        void UpdateMapMarkers() {
            if (!ParentModel.ShowQuestMarkers) {
                return;
            }

            RemoveElementsOfType<MapMarker>();
            if (State is not ObjectiveState.Active) {
                return;
            }

            var currentScene = Services.Get<SceneService>().ActiveSceneRef;
            for (int i = 0; i < MarkersData.Length; i++) {
                if (!MarkersData[i].MarkerVisible) {
                    continue;
                }
                
                var targets = GetTargets(MarkersData[i], Hero.Current, currentScene);
                foreach (var target in targets) {
                    int order = ParentModel.IsTracked ? MapMarkerOrder.TrackedQuest.ToInt() : MapMarkerOrder.Quest.ToInt();
                    var icon = QuestMarker.GetCompassMarkerIcon(target, ParentModel.IsTracked, ParentModel.QuestType);
                    if (target.TryGetElement(out LocationArea area)) {
                        bool isAlreadyAdded = false;
                        foreach (var areaMapMarker in Elements<QuestAreaMapMarker>()) {
                            if (areaMapMarker.Area == area) {
                                isAlreadyAdded = true;
                                break;
                            }
                        }
                        if (!isAlreadyAdded) {
                            AddElement(new QuestAreaMapMarker(area, DisplayName, icon, order, ParentModel.IsTracked));
                        }
                    } else {
                        var grounded = MapMarkerTarget(target);
                        bool isAlreadyAdded = false;
                        foreach (var pointMapMarker in Elements<PointMapMarker>()) {
                            if (pointMapMarker.Grounded == grounded) {
                                isAlreadyAdded = true;
                                break;
                            }
                        }
                        if (!isAlreadyAdded) {
                            AddElement(new PointMapMarker(new WeakModelRef<IGrounded>(grounded), DisplayName, icon, order, true, highlightAnimation: ParentModel.IsTracked));
                        }
                    }

                    string DisplayName() {
                        return ParentModel.DisplayName;
                    }
                }
            }
        }
        
        void TryDetachPresenceTracker() {
            for (int i = 0; i < MarkersData.Length; i++) {
                if (MarkersData[i].presenceTracker is {HasBeenDiscarded: false}) {
                    MarkersData[i].presenceTracker.PresenceUpdated -= TargetSceneHasChanged;
                }
            }
        }
        
        void TryAttachPresenceTracker(bool withUpdate = true) {
            for(int i = 0; i < MarkersData.Length; i++) {
                if (MarkersData[i].presenceTracker is { HasBeenDiscarded: false }) {
                    return;
                }

                if (MarkersData[i].LocationReference.TargetsActors && MarkersData[i].LocationReference.actors.IsNotNullOrEmpty()) {
                    MarkersData[i].presenceTracker = PresenceTrackerService.TrackerFor(MarkersData[i].LocationReference.actors[0]);
                    if (MarkersData[i].presenceTracker is { HasBeenDiscarded: false }) {
                        MarkersData[i].presenceTracker.PresenceUpdated += TargetSceneHasChanged;
                        if (withUpdate) {
                            TriggerCompassMarkerUpdate();
                        }
                    }
                }
            }
        }

        protected override void OnDiscard(bool fromDomainDrop) {
            TryDetachPresenceTracker();
            base.OnDiscard(fromDomainDrop);
        }

        // === Targets
        void TargetSceneHasChanged() {
            UpdateMarkersAfterLoad()
                .ContinueWith(TriggerCompassMarkerUpdate)
                .Forget();
        }

        void TriggerCompassMarkerUpdate() {
            if (ParentModel is not { HasBeenDiscarded: false } || HasBeenDiscarded) {
                return;
            }
            ParentModel.Trigger(
                QuestUtils.Events.ObjectiveChanged,
                new QuestUtils.ObjectiveStateChange { objective = this, oldState = State, newState = State });
        }

        public List<IGrounded> GetAllActiveTargets(IGrounded from, SceneReference currentScene) {
            var result = new List<IGrounded>();
            for (int i = 0; i < MarkersData.Length; i++) {
                if (MarkersData[i].MarkerVisible) {
                    result.AddRange(GetTargets(MarkersData[i], from, currentScene));
                }
            }
            return result;
        }

        public static IEnumerable<IGrounded> GetTargets(MarkerData data, IGrounded from, SceneReference currentScene) {
            if (data.LocationReference is not { IsSet: true }) {
                yield break;
            }

            if (currentScene == data.TargetScene) {
                foreach (IGrounded target in GetTargetsFromScene(data)) {
                    yield return target;
                }
            } else if (TryGetPortalToObjective(data, from, out Portal portal)) {
                yield return portal.ParentModel;
            } else if (!World.Services.Get<SceneService>().IsOpenWorld) {
                portal = Portal.FindClosestExit(from, true);
                if (portal != null) {
                    yield return portal.ParentModel;
                }
            }
        }

        public static IEnumerable<IGrounded> GetTargetsFromScene(MarkerData data) {
            return data.LocationReference.MatchingLocations(null);
        }
        
        static bool TryGetPortalToObjective(MarkerData data, IGrounded from, out Portal portal) {
            portal = Portal.FindClosest(data.TargetScene, from, false, true);

            return portal != null;
        }

        IGrounded MapMarkerTarget(IGrounded grounded) {
            if (grounded is Location location) {
                return (IGrounded)location.TryGetElement<NpcElement>()?.TryGetElement<NpcTargetGrounded>() ?? location;
            }
            return grounded;
        }
        
        // === Helpers
        public string GetQuestLogDescription() {
            if (State is ObjectiveState.Completed or ObjectiveState.Failed) {
                return Description;
            }
            string baseDesc = Description;
            string trackersDesc = GetTrackersDescription();
            return $"{baseDesc} {trackersDesc}";
        }
        
        public string GetQuestTrackerDescription() {
            if (State is ObjectiveState.Completed or ObjectiveState.Failed) {
                return TrackerDescription.toggled ? TrackerDescription.LocString : Description;
            }
            if (TryGetTrackerDescriptionOverride(out string baseDesc)) {
                return baseDesc;
            }
            baseDesc = TrackerDescription.toggled ? TrackerDescription.LocString : Description;
            string trackersDesc = GetTrackersDescription();
            return $"{baseDesc} {trackersDesc}";
        }

        bool TryGetTrackerDescriptionOverride(out string baseDesc) {
            foreach (var tracker in Trackers) {
                if (tracker.DescriptionOverride != null) {
                    baseDesc = tracker.DescriptionOverride;
                    return true;
                }
            }
            baseDesc = null;
            return false;
        }

        string GetTrackersDescription() {
            var trackers = Trackers;

            IEnumerable<string> trackersDesc = trackers.Select(tracker => {
                string desc = tracker.TrackingDesc();

                if (string.IsNullOrWhiteSpace(desc)) {
                    return string.Empty;
                }
                
                // add a dot to the beginning of the description if it is item tracker using ItemCurByMax pattern
                if (tracker is ItemTracker itemTracker && itemTracker.DisplayPattern == TrackerDisplayPattern.ItemCurByMax) {
                    desc = desc.WithSprite("_ui_dot_white", ARColor.MainGrey);
                }
                
                if (tracker.StartDisplayPatternInNewLine) {
                    desc = $"\n{desc}";
                }
                
                return desc;
            });
            
            return trackers.Any() ? string.Join('\n', trackersDesc) : string.Empty;
        }

        [Serializable]
        public struct MarkerData {
            internal PresenceTracker presenceTracker;
            SceneReference _fallbackTargetScene;

            /// <summary>
            /// Tells if marker visibility depends on story flag
            /// </summary>
            public bool IsMarkerRelatedToStory { get; private set; }
            /// <summary>
            /// Story flag that controls marker visibility
            /// </summary>
            public FlagLogic RelatedStoryFlag { get; private set; }
            /// <summary>
            /// Location reference might get used for drawing minimap path to target location
            /// </summary>
            public LocationReference LocationReference { get; private set; }
            public SceneReference TargetScene {
                get {
                    if (presenceTracker is {HasBeenDiscarded: false} tracker) {
                        return tracker.CurrentScene;
                    }
                    return _fallbackTargetScene;
                }
            }
            
            public bool MarkerVisible => !IsMarkerRelatedToStory || RelatedStoryFlag.Get();
            public bool HasSetupMarker => LocationReference is { IsSet: true } && TargetScene is { IsSet: true };

            public SceneReference OpenWorldScene => ScenesCache.Get.GetOpenWorldRegion(TargetScene);
            
            public MarkerData(bool isMarkerRelatedToStory, FlagLogic relatedStoryFlag, LocationReference locationReference, SceneReference targetScene) {
                IsMarkerRelatedToStory = isMarkerRelatedToStory;
                RelatedStoryFlag = relatedStoryFlag;
                LocationReference = locationReference;
                _fallbackTargetScene = targetScene;
                presenceTracker = null;
            }
        }
    }
}