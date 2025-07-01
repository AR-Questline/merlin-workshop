using System;
using System.Collections.Generic;
using System.Linq;
using Awaken.TG.Graphics;
using Awaken.TG.Main.AI.Idle.Data.Attachment;
using Awaken.TG.Main.AI.Idle.Finders;
using Awaken.TG.Main.Fights.NPCs.Presences;
using Awaken.TG.Main.Locations;
using Awaken.TG.Main.Locations.Attachments;
using Awaken.TG.Main.Locations.Spawners;
using Awaken.TG.Main.Timing;
using Awaken.TG.MVC;
using Awaken.TG.MVC.Elements;
using Awaken.Utility;
using Awaken.Utility.Debugging;
using Newtonsoft.Json;
using Sirenix.Utilities;
using Unity.IL2CPP.CompilerServices;
using UnityEngine;
using InteractionData = Awaken.TG.Main.AI.Idle.Data.Attachment.InteractionData;
using LogType = Awaken.Utility.Debugging.LogType;

namespace Awaken.TG.Main.AI.Idle.Data.Runtime {
    [Il2CppEagerStaticClassConstruction]
    public sealed partial class IdleDataElement : Element<Location>, IRefreshedByAttachment<IdleDataAttachment>, IIdleDataSource {
        public override ushort TypeForSerialization => SavedModels.IdleDataElement;

        static readonly List<InteractionInterval> IntervalsList = new();
        static readonly List<InteractionOneShotData> OneShotsList = new();
        static readonly InteractionInterval[] FallbackIntervals = { InteractionInterval.Fallback };
        static DateTime CurrentTime => (DateTime)World.Only<GameRealTime>().WeatherTime;
        
        IdleDataAttachment _spec;
        NamedInteractionSource[] _customActions;
        InteractionInterval[] _intervals;
        InteractionOneShotData[] _oneShots;
        TimedEvent _nextIntervalEvent;
        TimedEvent _nextOneShotEvent;

        public int Priority => 0;
        public float PositionRange { get; private set; }

        public bool UseAttachmentSpace => ParentModel.HasElement<NpcPresence>() || ParentModel.HasElement<BaseLocationSpawner>();
        public Vector3 AttachmentPosition => _spec != null ? _spec.transform.position : Vector3.zero;
        public Vector3 AttachmentForward =>  _spec != null ? _spec.transform.forward : Vector3.zero;
        public FallbackInteractionData FallbackInteractionData => _spec != null ? _spec.FallbackInteractionData : FallbackInteractionData.Default;

        public void InitFromAttachment(IdleDataAttachment spec, bool isRestored) {
            _spec = spec;
            ref readonly var data = ref spec.Data;
            _customActions = CreateCustomActions(data);
            _intervals = CreateIntervals(data);
            _oneShots = CreateOneShots(data);
            PositionRange = spec.PositionRange;
        }

        protected override void OnInitialize() {
            OnIntervalChanged();
            OneShotsInit();
            World.Only<WeatherController>().HeavyRainStateChanged += OnPrecipitationInteractionStateChanged;
        }

        void OnPrecipitationInteractionStateChanged() {
            ParentModel.Trigger(IIdleDataSource.Events.InteractionIntervalChanged, this);
        }

        protected override void OnDiscard(bool fromDomainDrop) {
            var weatherController = World.Any<WeatherController>();
            if (weatherController != null) {
                weatherController.HeavyRainStateChanged -= OnPrecipitationInteractionStateChanged;
            }

            var gameTimeEvents = World.Any<GameTimeEvents>();
            if (gameTimeEvents != null) {
                gameTimeEvents.RemoveEvent(_nextIntervalEvent);
                gameTimeEvents.RemoveEvent(_nextOneShotEvent);
            }
            _nextIntervalEvent = null;
            _nextOneShotEvent = null;
        }

        void OnIntervalChanged() {
            var nextStart = GetNextIntervalStartTime(CurrentTime, true);
            _nextIntervalEvent = new TimedEvent(nextStart, OnIntervalChanged);
            World.Any<GameTimeEvents>().AddEvent(_nextIntervalEvent);
            ParentModel.Trigger(IIdleDataSource.Events.InteractionIntervalChanged, this);
        }

        void OneShotsInit() {
            if (_oneShots == null) {
                return;
            }
            PrepareNextOneShot();
        }

        void OnOneShotTriggered() {
            var currentOneShotIndex = GetCurrentOneShotIndex(CurrentTime);
            PrepareNextOneShot();
            ParentModel.Trigger(IIdleDataSource.Events.InteractionOneShotTriggered, _oneShots[currentOneShotIndex]);
        }
        
        void PrepareNextOneShot() {
            _nextOneShotEvent = new TimedEvent(GetNextOneShotStartTime(CurrentTime, true), OnOneShotTriggered);
            World.Any<GameTimeEvents>().AddEvent(_nextOneShotEvent);
        }

        NamedInteractionSource[] CreateCustomActions(in IdleData data) {
            if (data.customActions is not { Length: > 0 }) {
                return Array.Empty<NamedInteractionSource>();
            }
            var customActions = new NamedInteractionSource[data.customActions.Length];
            for (int i = 0; i < data.customActions.Length; i++) {
                customActions[i] = new NamedInteractionSource(data.customActions[i].name, data.customActions[i].action, this);
            }
            return customActions;
        }
        
        InteractionInterval[] CreateIntervals(in IdleData data) {
            if (data.behaviours.Length == 0) {
                return FallbackIntervals;
            }
            
            IntervalsList.Clear();
            //data.behaviours.Sort(InteractionIntervalData.CompareDate);
            
            for (int i = 0; i < data.behaviours.Length - 1; i++) {
                data.behaviours[i].AppendIntervals(this, IntervalsList, data.behaviours[i + 1], false);
            }
            data.behaviours[^1].AppendIntervals(this, IntervalsList, data.behaviours[0], true);
            IntervalsList.Sort(InteractionInterval.CompareDate);
            
            var behaviours = IntervalsList.ToArray();
            IntervalsList.Clear();
            return behaviours;
        }
        
        InteractionOneShotData[] CreateOneShots(in IdleData data) {
            if (data.oneShots == null || data.oneShots.Length == 0) {
                return null;
            }
            
            OneShotsList.Clear();
            for (int i = 0; i < data.oneShots.Length; i++) {
                data.oneShots[i].AppendOneShots(this, OneShotsList);
            }
            var oneShots = OneShotsList.OrderBy(o => o.Hour).ToArray();
            OneShotsList.Clear();
            return oneShots;
        }

        public InteractionSource GetCustomAction(string name) {
            for (int i = 0; i < _customActions.Length; i++) {
                if (_customActions[i].name == name) {
                    return _customActions[i].source;
                }
            }
            Log.Important?.Error($"Cannot find CustomAction with name {name} on IdleDataAttachment {_spec}", _spec);
            return null;
        }
        
        public IInteractionSource GetCurrentSource() {
            return _intervals[GetCurrentIntervalIndex(CurrentTime)].GetCurrentSource();
        }

        int GetCurrentIntervalIndex(DateTime currentTime) {
            for (int i = 0; i < _intervals.Length; i++) {
                var start = _intervals[i].ThisDayStartTime(currentTime);
                if (start > currentTime) {
                    return i == 0 ? _intervals.Length - 1 : i - 1;
                }
            }
            return _intervals.Length - 1;
        }

        DateTime GetNextIntervalStartTime(DateTime currentTime, bool withDeviation) {
            for (int i = 0; i < _intervals.Length; i++) {
                var start = _intervals[i].ThisDayStartTime(currentTime);
                if (start > currentTime) {
                    return _intervals[i].ThisDayStartTime(currentTime, withDeviation);
                }
            }
            return _intervals[0].ThisDayStartTime(currentTime, withDeviation).AddDays(1);
        }
        
        int GetCurrentOneShotIndex(DateTime currentTime) {
            for (int i = 0; i < _oneShots.Length; i++) {
                var start = _oneShots[i].ThisDayStartTime(currentTime);
                if (start > currentTime) {
                    return i - 1 >= 0 ? i - 1 : _oneShots.Length - 1;
                }
            }
            return _oneShots.Length - 1;
        }
        
        DateTime GetNextOneShotStartTime(DateTime currentTime, bool withDeviation) {
            for (int i = 0; i < _oneShots.Length; i++) {
                var start = _oneShots[i].ThisDayStartTime(currentTime);
                if (start > currentTime) {
                    return _oneShots[i].ThisDayStartTime(currentTime, withDeviation);
                }
            }
            return _oneShots[0].ThisDayStartTime(currentTime, withDeviation).AddDays(1);
        }

        readonly struct NamedInteractionSource {
            public readonly string name;
            public readonly InteractionSource source;

            public NamedInteractionSource(string name, in InteractionData data, IdleDataElement element) {
                this.name = name;
                source = new InteractionSource(data.CreateFinder(element), FallbackInteractionData.Default);
            }
        }
    }
}