using Awaken.Utility;
using System;
using System.Collections.Generic;
using Awaken.TG.Main.Character;
using Awaken.TG.Main.Fights.DamageInfo;
using Awaken.TG.Main.Fights.NPCs;
using Awaken.TG.Main.Locations;
using Awaken.TG.MVC;
using Awaken.TG.MVC.Elements;
using Awaken.TG.MVC.Events;
using CountType = Awaken.TG.Main.Stories.Quests.Objectives.Trackers.LocationSceneSpecificTrackerAttachment.CountType;

namespace Awaken.TG.Main.Stories.Quests.Objectives.Trackers {
    public partial class LocationSceneSpecificTracker : BaseSceneSpecificTracker<LocationSceneSpecificTrackerAttachment> {
        public override ushort TypeForSerialization => SavedModels.LocationSceneSpecificTracker;

        LocationReference[] _locationsToTrack;
        bool _countOnlyAlive;
        int _amountToComplete;
        CountType _countType;
        
        int _counter;
        bool _counterActive;
        IEventListener _modelAddedListener;
        Dictionary<Location, LocationListeners> _countedData;

        protected override bool TrackerCompleted => _counterActive && IsCorrectAmount;

        bool IsCorrectAmount => _countType switch {
                CountType.LesserOrEqual => _counter <= _amountToComplete,
                CountType.Equal => _counter == _amountToComplete,
                CountType.GreaterOrEqual => _counter >= _amountToComplete,
                _ => throw new ArgumentOutOfRangeException()
            };

        public override void InitFromAttachment(LocationSceneSpecificTrackerAttachment spec, bool isRestored) {
            base.InitFromAttachment(spec, isRestored);
            _locationsToTrack = spec.locationsToTrack;
            _countOnlyAlive = spec.countOnlyAlive;
            _amountToComplete = spec.amountToComplete;
            _countType = spec.countType;
        }
        
        // === Listeners
        protected override void OnCorrectSceneEntered() {
            _counter = 0;
            _countedData ??= new Dictionary<Location, LocationListeners>();
            foreach (var locationRef in _locationsToTrack) {
                foreach (var location in locationRef.MatchingLocations(null)) {
                    AddLocation(location);
                }
            }
            _modelAddedListener = World.EventSystem.ListenTo(EventSelector.AnySource, World.Events.ModelAdded<Location>(), this, OnModelAdded);
        }

        protected override void OnCorrectSceneLeft() {
            _counterActive = false;
            foreach (var data in _countedData) {
                World.EventSystem.RemoveListener(data.Value.discardListener);
                if (data.Value.killListener != null) {
                    World.EventSystem.RemoveListener(data.Value.killListener);
                }
            }
            _countedData.Clear();
            _counter = 0;
            World.EventSystem.DisposeListener(ref _modelAddedListener);
        }

        void OnModelAdded(Model obj) {
            if (obj is not Location location) {
                return;
            }

            foreach (var locationRef in _locationsToTrack) {
                if (locationRef.IsMatching(null, location)) {
                    AddLocation(location);
                    return;
                }
            }
        }

        void OnLocationKilled(DamageOutcome outcome) {
            Location location = outcome.Target is Element<Location> element ? element.ParentModel : null;
            if (location == null) {
                return;
            }
            RemoveLocation(location);
        }

        void OnLocationDiscarded(Model obj) {
            if (obj is not Location location) {
                return;
            }
            RemoveLocation(location);
        }
        
        // === Operations
        void AddLocation(Location location) {
            IAlive alive = null;
            if (_countOnlyAlive) {
                if (location.TryGetElement<IAlive>(out alive)) {
                    if (!alive.IsAlive || alive.IsDying) {
                        return;
                    }
                } else if (location.TryGetElement<NpcDummy>(out var dummy)) {
                    if (dummy.HasDied) {
                        return;
                    }
                }
            }

            var listeners = new LocationListeners() {
                discardListener = location.ListenTo(Model.Events.BeforeDiscarded, OnLocationDiscarded, this),
                killListener = _countOnlyAlive
                    ? alive?.ListenTo(IAlive.Events.BeforeDeath, OnLocationKilled, this)
                    : null
            };
            _countedData.Add(location, listeners);
            IncreaseCounter();
        }

        void RemoveLocation(Location location) {
            if (_countedData.TryGetValue(location, out var listeners)) {
                World.EventSystem.DisposeListener(ref listeners.discardListener);
                World.EventSystem.DisposeListener(ref listeners.killListener);
                _countedData.Remove(location);
                if (!location.WasDiscardedFromDomainDrop) {
                    DecreaseCounter();
                }
            }
        }

        void IncreaseCounter() {
            _counterActive = true;
            _counter++;
            CheckIfCompleted();
            TriggerChange();
        }

        void DecreaseCounter() {
            _counter--;
            CheckIfCompleted();
            TriggerChange();
        }
        
        // === Helpers
        protected override string OnSceneDesc() {
            return base.OnSceneDesc()
                .Replace("{cur}", _counter.ToString())
                .Replace("{max}", _amountToComplete.ToString());
        }

        struct LocationListeners {
            public IEventListener discardListener;
            public IEventListener killListener;
        }
    }
}