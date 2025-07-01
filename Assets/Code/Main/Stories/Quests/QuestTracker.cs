using System.Collections.Generic;
using System.Linq;
using Awaken.TG.Assets;
using Awaken.TG.Main.Grounds;
using Awaken.TG.Main.Heroes;
using Awaken.TG.Main.Locations;
using Awaken.TG.Main.Locations.Attachments.Elements;
using Awaken.TG.Main.Maps.Markers;
using Awaken.TG.Main.Stories.Quests.Objectives;
using Awaken.TG.Main.Stories.Quests.UI;
using Awaken.TG.Main.UI.TitleScreen.Loading;
using Awaken.TG.Main.Utility;
using Awaken.TG.MVC;
using Awaken.TG.MVC.Attributes;
using Awaken.TG.MVC.Domains;
using Awaken.TG.MVC.Elements;
using Awaken.TG.MVC.Events;
using Awaken.TG.MVC.UI;
using Awaken.TG.MVC.UI.Events;
using Awaken.TG.Utility.Attributes;
using Awaken.Utility;
using Awaken.Utility.Collections;
using Awaken.Utility.LowLevel.Collections;
using Unity.Collections;

namespace Awaken.TG.Main.Stories.Quests {
    [SpawnsView(typeof(VQuestTracker))]
    public partial class QuestTracker : Element<Hero>, IUIPlayerInput {
        public override ushort TypeForSerialization => SavedModels.QuestTracker;

        readonly List<Quest3DMarker> _3dMarkersBuffer = new();
        readonly List<QuestMarker> _markersBuffer = new();

        public IEnumerable<KeyBindings> PlayerKeyBindings => KeyBindings.UI.HUD.ToggleQuestTracker.Yield();

        // === State
        [Saved] public Quest ActiveQuest { get; private set; }

        public new static class Events {
            public static readonly Event<QuestTracker, Quest> QuestRefreshed = new(nameof(QuestRefreshed));
            public static readonly Event<QuestTracker, Quest> QuestDisplayed = new(nameof(QuestDisplayed));
            public static readonly Event<QuestTracker, Quest> QuestTracked = new(nameof(QuestTracked));
            public static readonly Event<QuestTracker, Quest> QuestTrackerClicked = new(nameof(QuestTrackerClicked));
            public static readonly Event<QuestTracker, Quest> QuestDisplayingInterrupted = new(nameof(QuestDisplayingInterrupted));
        }
        
        // === Initialization
        protected override void OnInitialize() {
            this.ListenTo(Model.Events.AfterFullyInitialized, Init, this);
        }

        void Init() {
            World.Only<PlayerInput>().RegisterPlayerInput(this, this);
            World.EventSystem.ListenTo(EventSelector.AnySource, QuestUtils.Events.QuestStateChanged, this, QuestStateChanged);
            World.EventSystem.ListenTo(EventSelector.AnySource, QuestUtils.Events.ObjectiveChanged, this, OnObjectiveStateChanged);
            World.EventSystem.ListenTo(EventSelector.AnySource, QuestUtils.Events.ActiveObjectivesEstablished, this, OnActiveObjectivesEstablished);
            World.EventSystem.ListenTo(EventSelector.AnySource, LoadingScreenUI.Events.SceneInitializationEnded, this, RefreshActiveQuest);
            World.EventSystem.ListenTo(EventSelector.AnySource, QuestUtils.Events.ObjectiveRelatedStoryFlagChanged, this, RefreshActiveQuest);

            if (ActiveQuest == null) {
                Quest trackedQuest = null;
                Quest trackableQuest = null;
                foreach (var quest in World.All<Quest>()) {
                    if (quest.CanBeTracked && quest.State == QuestState.Active) {
                        trackableQuest ??= quest;
                        if (quest.IsTracked) {
                            trackedQuest = quest;
                            break;
                        }
                    }
                }
                trackedQuest ??= trackableQuest;

                if (trackedQuest != null) {
                    Track(trackedQuest);
                }
            }
        }

        void RefreshActiveQuest() {
            UpdateActiveQuest(false);
            this.Trigger(Events.QuestRefreshed, ActiveQuest);
        }

        void QuestStateChanged(QuestUtils.QuestStateChange stateChange) {
            var quest = stateChange.quest;
            if (ActiveQuest == quest && (ActiveQuest.State == QuestState.Completed || ActiveQuest.State == QuestState.Failed)) {
                ActiveQuest = null;
                Quest viableQuest = World.All<Quest>().ToArraySlow().OrderBy(q => q.QuestType).FirstOrDefault(q => q.CanBeTracked && q.State == QuestState.Active);
                if (viableQuest != null) {
                    ToggleActiveQuest(viableQuest);
                }
            }
        }
        
        void OnObjectiveStateChanged(QuestUtils.ObjectiveStateChange stateChange) {
            if (ActiveQuest != stateChange.objective.ParentModel) {
                return;
            }
            
            RefreshTrackableObjectives();
            this.Trigger(Events.QuestRefreshed, ActiveQuest);
            UpdateMarkers(ActiveQuest);
        }

        // === Public API
        public void Track(Quest quest) {
            if (ActiveQuest == quest || quest is {CanBeTracked: false}) {
                return;
            }
            
            ToggleActiveQuest(quest);
            UpdateActiveQuest(true);
        }
        
        void ToggleActiveQuest(Quest quest) {
            ActiveQuest?.ToggleTracking(false);
            quest?.ToggleTracking(true);
            ActiveQuest = quest;
        }
        
        void RefreshTrackableObjectives() {
            var currentTrackers = Elements<QuestTrackerObjective>();
            var usedMask = new UnsafeBitmask(currentTrackers.Count(), ARAlloc.Temp);
            usedMask.All();

            foreach (var quest in World.All<Quest>()) {
                if (!quest.CanBeTracked || quest.State != QuestState.Active) {
                    continue;
                }
                foreach (var objective in quest.Objectives) {
                    if (objective.State != ObjectiveState.Active) {
                        continue;
                    }
                    var index = currentTrackers.IndexOf(new QuestTrackerObjective.ObjectiveComparer(objective));
                    if (index == -1) {
                        if (string.IsNullOrEmpty(objective.GetQuestTrackerDescription()) == false) {
                            AddElement(new QuestTrackerObjective(objective));
                        }
                    } else {
                        usedMask.Down((uint)index);
                    }
                }
            }

            foreach (var index in usedMask.EnumerateOnes()) {
                currentTrackers.At(index).TryToDiscard();
            }
        }

        // === Callbacks
        void OnActiveObjectivesEstablished(Quest quest) {
            if (ActiveQuest == null) {
                Track(quest);
            }
        }

        void UpdateActiveQuest(bool questTracked) {
            RefreshTrackableObjectives();

            if (ActiveQuest != null) {
                UpdateMarkers(ActiveQuest);
                this.Trigger(questTracked ? Events.QuestTracked : Events.QuestRefreshed, ActiveQuest);
            } else {
                RemoveMarkers();
            }
        }

        void UpdateMarkers(Quest quest) {
            SceneReference currentScene = Services.Get<SceneService>().ActiveSceneRef;
            RemoveMarkers();

            if (!quest?.ShowQuestMarkers ?? true) {
                return;
            }
            
            foreach (Objective activeObjective in quest.ActiveObjectivesWithMarkers) {
                int i = 1;
                var targets = activeObjective.GetAllActiveTargets(ParentModel, currentScene);
                foreach (IGrounded target in targets) {
                    AddMarkers(target, i, targets.Count > 1, quest.QuestType);
                    i++;
                }
            }
        }

        static void AddMarkers(IGrounded target, int orderNumber, bool isNumberVisible, QuestType questType) {
            if (target is Location location) {
                var icon = QuestMarker.GetCompassMarkerIcon(location, true, questType);
                var marker = new QuestMarker(icon, isNumberVisible);
                location.AddElement(marker);
                marker.CompassElement.SetShowDistance(true);
                if (!location.HasElement<LocationArea>()) {
                    var icon3d = QuestMarker.Get3DMarkerIcon(location, true);
                    World.Add(new Quest3DMarker(target, icon3d, orderNumber, isNumberVisible));
                }
            }
        }

        void RemoveMarkers() {
            World.All<Quest3DMarker>().FillList(_3dMarkersBuffer);
            foreach (var marker in _3dMarkersBuffer) {
                marker.Discard();
            }
            _3dMarkersBuffer.Clear();

            World.All<QuestMarker>().FillList(_markersBuffer);
            foreach (var objectiveMarker in _markersBuffer) {
                objectiveMarker.Discard();
            }
            _markersBuffer.Clear();
        }

        public UIResult Handle(UIEvent evt) {
            if (evt is UIKeyDownAction) {
                this.Trigger(Events.QuestTrackerClicked, ActiveQuest);
                return UIResult.Accept;
            }

            return UIResult.Ignore;
        }
    }
}