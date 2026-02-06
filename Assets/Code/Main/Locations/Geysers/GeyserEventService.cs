using Awaken.TG.Main.Grounds.CullingGroupSystem;
using Awaken.TG.Main.Timing;
using Awaken.TG.MVC;
using Awaken.TG.MVC.Domains;
using Awaken.Utility.Collections;
using JetBrains.Annotations;

namespace Awaken.TG.Main.Locations.Geysers {
    public class GeyserEventService : IDomainBoundService, UnityUpdateProvider.IWithUpdateGeneric {
        const int SmoothTransitionMaxBandThreshold = 3;

        public Domain Domain => Domain.CurrentMainScene();

        StructList<GeyserEvent> _events;
        StructList<ICullingSystemRegistree> _cullingRegistrees;
        GameRealTime _realTime;
        CullingSystem _cullingSystem;
        int _nextEventIndex = -1;
        
        GameRealTime RealTime => _realTime ??= World.Any<GameRealTime>();
        CullingSystem CullingSystem => _cullingSystem ?? World.Services.Get<CullingSystem>();
        
        [CanBeNull]
        public static GeyserEventService TryGet() {
            return World.Services.TryGet<GeyserEventService>();
        }

        public static GeyserEventService GetOrCreate() {
            var controller = TryGet();
            if (controller == null) {
                controller = new GeyserEventService();
                World.Services.Register(controller);
                UnityUpdateProvider.GetOrCreate().RegisterGeneric(controller);
            }
            return controller;
        }

        public void RegisterGeyser(GeyserEvent geyserEvent) {
            if (!_events.IsCreated) {
                _events = new StructList<GeyserEvent>(10);
                _cullingRegistrees = new StructList<ICullingSystemRegistree>(10);
            }
            _events.Add(geyserEvent);
            _cullingRegistrees.Add(geyserEvent.geyser.ParentModel);
            if (_nextEventIndex < 0 || geyserEvent.eventTime < _events[_nextEventIndex].eventTime) {
                _nextEventIndex = _events.Count - 1;
            }
        }

        public void UnregisterGeyser(GeyserElement geyser) {
            for (int i = _events.Count - 1; i >= 0; i--) {
                if (_events[i].geyser == geyser) {
                    _events.RemoveAtSwapBack(i);
                    _cullingRegistrees.RemoveAtSwapBack(i);
                    if (_nextEventIndex >= i) {
                        _nextEventIndex = -1;
                        for (int j = 0; j < _events.Count; j++) {
                            if (_nextEventIndex < 0 || _events[j].eventTime < _events[_nextEventIndex].eventTime) {
                                _nextEventIndex = j;
                            }
                        }
                    }
                    break;
                }
            }
        }
        
        public bool IsGeyserRegistered(GeyserElement geyser) {
            for (int i = 0; i < _events.Count; i++) {
                if (_events[i].geyser == geyser) {
                    return true;
                }
            }
            return false;
        }
        
        public void UnityUpdate() {
            if (_nextEventIndex < 0 || _events.Count == 0) {
                return;
            }
            
            var realTime = RealTime.PlayRealTimeInSeconds;
            if (realTime <= _events[_nextEventIndex].eventTime) {
                return;
            }
            double nextEventTime = double.MaxValue;
            for (int i = 0; i < _events.Count; i++) {
                if (_events[i].eventTime < realTime) {
                    OnGeyserTrigger(i);
                }
                if (_events[i].eventTime < nextEventTime) {
                    nextEventTime = _events[i].eventTime;
                    _nextEventIndex = i;
                }
            }
        }

        void OnGeyserTrigger(int index) {
            if (_events[index].activate) {
                _events[index] = _events[index].geyser.ActivateAndGetNextEvent(CullingSystem.GetDistanceBand(_cullingRegistrees[index]) > SmoothTransitionMaxBandThreshold);
            } else {
                _events[index] = _events[index].geyser.DeactivateAndGetNextEvent(CullingSystem.GetDistanceBand(_cullingRegistrees[index]) > SmoothTransitionMaxBandThreshold);
            }
        }
        
        public bool RemoveOnDomainChange() {
            Discard();
            return true;
        }

        void Discard() {
            UnityUpdateProvider.TryGet()?.UnregisterGeneric(this);
        }

        public struct GeyserEvent {
            public GeyserElement geyser;
            public double eventTime;
            public bool activate;
            
            public GeyserEvent(GeyserElement geyser, double eventTime, bool activate) {
                this.geyser = geyser;
                this.eventTime = eventTime;
                this.activate = activate;
            }
        }
    }
}