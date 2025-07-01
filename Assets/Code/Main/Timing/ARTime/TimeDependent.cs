using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using Animancer;
using Awaken.TG.Main.Fights.Utils;
using Awaken.TG.Main.Timing.ARTime.Modifiers;
using Awaken.TG.Main.Timing.ARTime.TimeComponents;
using Awaken.TG.MVC;
using Awaken.TG.MVC.Elements;
using Awaken.Utility.Collections;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace Awaken.TG.Main.Timing.ARTime {
    /// <summary>
    /// Element that provides custom time scale for its parent and views of its parent
    /// </summary>
    public sealed partial class TimeDependent : Element {
        static readonly List<ITimeModifier> TimeModifiers = new(5);
        static Update[] s_updates = new Update[80];

        public override bool IsNotSaved => true;

        public delegate void Update(float deltaTime);
        public delegate void TimeScaleChanged(float from, float to);
        event TimeScaleChanged OnTimeScaleChanged;

        readonly List<ITimeComponent> _timeComponents = new();
        StructList<Update> _updates = new(0);
        StructList<Update> _alwaysUpdates = new(0);
        StructList<Update> _lateUpdates = new(0);
        StructList<Update> _fixedUpdates = new(0);
        ITimeDependentDisabler _parentDisabler;
        bool _updateDelegateChanged, _alwaysUpdateDelegateChanged, _lateUpdateDelegateChanged, _fixedUpdateDelegateChanged;

        public bool IgnoreGameRealTimeModifiers { get; private set; }
        
        public float TimeScaleModifier { get; private set; }
        public float DeltaTime => Time.deltaTime * TimeScaleModifier;
        public float FixedDeltaTime => Time.fixedDeltaTime * TimeScaleModifier;

        public bool IsPaused => Time.timeScale == 0 || TimeScaleModifier == 0;
        public bool CanProcess => !WasDiscarded && (!IsPaused || ProcessWhenPause) && _parentDisabler is not {TimeUpdatesDisabled: true};
        public bool ProcessWhenPause { get; private set; }

        public bool HasUpdate => _updates.Count > 0;
        public bool HasLateUpdate => _lateUpdates.Count > 0;
        public bool HasFixedUpdate => _fixedUpdates.Count > 0;

        event Update OnUpdate {
            add => _updates.Add(value);
            remove => _updates.Remove(value);
        }
        
        event Update OnAlwaysUpdate {
            add => _alwaysUpdates.Add(value);
            remove => _alwaysUpdates.Remove(value);
        }
        event Update OnLateUpdate {
            add => _lateUpdates.Add(value);
            remove => _lateUpdates.Remove(value);
        }
        event Update OnFixedUpdate{
            add => _fixedUpdates.Add(value);
            remove => _fixedUpdates.Remove(value);
        }
        
        // == Initialization
        
        protected override void OnInitialize() {
            _parentDisabler = GenericParentModel as ITimeDependentDisabler;
            TimeScaleModifier = 1;
            ModelUtils.DoForFirstModelOfType<GameRealTime>(OnGameRealTimeSpawned, this);
            // Time Components
            OnFixedUpdate += TCFixedUpdate;
            OnTimeScaleChanged += TCTimeScaleChanged;
        }

        void OnGameRealTimeSpawned(GameRealTime gameRealTime) {
            gameRealTime.TimeScaleChanged += RefreshTimeScale;
        }

        // == TimeComponent callbacks

        void TCFixedUpdate(float fixedDeltaTime) {
            RemoveInvalidComponents();
            foreach (ITimeComponent t in _timeComponents) {
                t.OnFixedUpdate(fixedDeltaTime);
            }
        }

        void TCTimeScaleChanged(float from, float to) {
            RemoveInvalidComponents();
            foreach (ITimeComponent t in _timeComponents) {
                t.OnTimeScaleChange(from, to);
            }
        }

        // == Modifiers
        
        public void AddTimeModifier(ITimeModifier modifier) {
            AddElement(modifier);
            modifier.Apply();
        }
        public void RemoveTimeModifiersFor(string sourceID) {
            foreach (var timeModifier in Elements<ITimeModifier>().Reverse()) {
                if (timeModifier.SourceID.Equals(sourceID)) {
                    timeModifier.Remove();
                }
            }
        }

        // == TimeScale
        
        public void RefreshTimeScale() {
            float previousTimeScale = TimeScaleModifier;
            TimeScaleModifier = CalculateTimeScale();
            if (TimeScaleModifier != previousTimeScale) {
                OnTimeScaleChanged?.Invoke(previousTimeScale, TimeScaleModifier);
            }
        }

        float CalculateTimeScale() {
            TimeModifiers.Clear();
            foreach (var modifier in Elements<ITimeModifier>()) {
                TimeModifiers.Add(modifier);
            }

            if (!IgnoreGameRealTimeModifiers) {
                TimeDependent gameTimeDependent = World.Only<GameRealTime>().TryGetElement<TimeDependent>();
                if (gameTimeDependent != null) {
                    foreach (var modifier in gameTimeDependent.Elements<ITimeModifier>()) {
                        TimeModifiers.Add(modifier);
                    }
                }
            }

            TimeModifiers.Sort((m1, m2) => m1.Order - m2.Order);

            float scale = 1;
            foreach (var modifier in TimeModifiers) {
                scale = modifier.Modify(scale);
            }
            
            return scale;
        }
        
        // == Lifetime
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void ProcessUpdate() {
            if (_updates.Count <= 0) {
                return;
            }
            ProcessDelegates(_updates, ref _updateDelegateChanged, DeltaTime);
        }
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void ProcessAlwaysUpdate() {
            if (_alwaysUpdates.Count <= 0) {
                return;
            }
            ProcessDelegates(_alwaysUpdates, ref _alwaysUpdateDelegateChanged, DeltaTime);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void ProcessLateUpdate() {
            if (_lateUpdates.Count <= 0) {
                return;
            }
            ProcessDelegates(_lateUpdates, ref _lateUpdateDelegateChanged, DeltaTime);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void ProcessFixedUpdate() {
            if (_fixedUpdates.Count <= 0) {
                return;
            }
            _fixedUpdateDelegateChanged = false;
            ProcessDelegates(_fixedUpdates, ref _fixedUpdateDelegateChanged, FixedDeltaTime);
        }

        // We can't simply invoke updateDelegate?.Invoke(), because invocation list can change during invoking.
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        // ReSharper disable once RedundantAssignment
        static void ProcessDelegates(StructList<Update> updates, ref bool delegateChanged, float deltaTime) {
            delegateChanged = false;
            var updatesLength = updates.Count;
            if (updatesLength > s_updates.Length) {
                var resizeSize = (int) (updatesLength * 1.25f);
                s_updates = new Update[resizeSize];
            }
            updates.CopyTo(s_updates);

            for (int i = 0; i < updatesLength; i++) {
                // --- If there was update to delegate and If delegate is not in delegates array, skip it.
                if (delegateChanged && !updates.Contains(s_updates[i])) {
                    continue;
                }
                
                s_updates[i].Invoke(deltaTime);
            }
            Array.Clear(s_updates, 0, updatesLength);
            delegateChanged = false;
        }
        
        // == TimeComponents

        public TimeDependent WithTimeComponentsOf(GameObject go) {
            return WithTimeComponents(GetAllTimeComponents(go));
        }

        public TimeDependent WithoutTimeComponentsOf(GameObject go) {
            RemoveInvalidComponents();
            var timeComponents = GetAllTimeComponents(go);
            foreach (var timeComponent in timeComponents) {
                _timeComponents.Remove(timeComponent);
            }
            return this;
        }

        public TimeDependent WithTimeComponents(IEnumerable<ITimeComponent> components) {
            foreach (var component in components) {
                WithTimeComponent(component);
            }
            return this;
        }

        public TimeDependent WithTimeComponent(ITimeComponent component) {
            RemoveInvalidComponents();
            if (_timeComponents.All(c => c.Component.GetHashCode() != component.Component.GetHashCode())) {
                _timeComponents.Add(component);
                component.OnTimeScaleChange(1, TimeScaleModifier);
            }
            return this;
        }

        IEnumerable<ITimeComponent> GetAllTimeComponents(GameObject go) {
            return TimeRigidbodies(go)
                .Concat(TimeAnimators(go))
                .Concat(TimeAnimancers(go))
                .Concat(TimeTrailRenderers(go));
        }
        
        IEnumerable<ITimeComponent> TimeRigidbodies(GameObject go) => go.GetComponentsInChildren<Rigidbody>(true)
            .Where(rb => !rb.isKinematic)
            .Select(rb => new TimeRigidbody(rb));
        IEnumerable<ITimeComponent> TimeAnimators(GameObject go) => go.GetComponentsInChildren<Animator>(true)
            .Where(a => a.GetComponent<AnimancerComponent>() == null)
            .Select(a => new TimeAnimator(a));
        IEnumerable<ITimeComponent> TimeAnimancers(GameObject go) => go.GetComponentsInChildren<AnimancerComponent>(true)
            .Select(a => new TimeAnimancer(a));
        IEnumerable<ITimeComponent> TimeTrailRenderers(GameObject go) => go.GetComponentsInChildren<TrailRenderer>(true)
            .Select(r => new TimeTrailRenderer(r));

        void RemoveInvalidComponents() {
            for (int i = _timeComponents.Count - 1; i >= 0; i--) {
                if (_timeComponents[i].Component == null) {
                    _timeComponents.RemoveAt(i);
                }
            }
        }
        
        public async UniTaskVoid RemoveInvalidComponentsAfterFrame() {
            // if we just destroyed the component then we have to wait for the next frame todetect its removal
            if (!await AsyncUtil.DelayFrame(this)) {
                return;
            }

            RemoveInvalidComponents();
        }

        // == Events

        public TimeDependent WithUpdate(Update update) {
            OnUpdate -= update;
            OnUpdate += update;
            return this;
        }
        public TimeDependent WithAlwaysUpdate(Update update) {
            OnAlwaysUpdate -= update;
            OnAlwaysUpdate += update;
            return this;
        }
        public TimeDependent WithLateUpdate(Update lateUpdate) {
            OnLateUpdate -= lateUpdate;
            OnLateUpdate += lateUpdate;
            return this;
        }
        public TimeDependent WithFixedUpdate(Update fixedUpdate) {
            OnFixedUpdate -= fixedUpdate;
            OnFixedUpdate += fixedUpdate;
            return this;
        }
        public TimeDependent WithTimeScaleChanged(TimeScaleChanged timeScaleChanged) {
            OnTimeScaleChanged -= timeScaleChanged;
            OnTimeScaleChanged += timeScaleChanged;
            return this;
        }

        public TimeDependent ThatProcessWhenPause() {
            ProcessWhenPause = true;
            return this;
        }

        public TimeDependent WithoutUpdate(Update update) {
            OnUpdate -= update;
            _updateDelegateChanged = true;
            return this;
        }
        public TimeDependent WithoutAlwaysUpdate(Update update) {
            OnAlwaysUpdate -= update;
            _alwaysUpdateDelegateChanged = true;
            return this;
        }
        public TimeDependent WithoutLateUpdate(Update lateUpdate) {
            OnLateUpdate -= lateUpdate;
            _lateUpdateDelegateChanged = true;
            return this;
        }
        public TimeDependent WithoutFixedUpdate(Update fixedUpdate) {
            OnFixedUpdate -= fixedUpdate;
            _fixedUpdateDelegateChanged = true;
            return this;
        }
        public TimeDependent WithoutTimeScaleChanged(TimeScaleChanged timeScaleChanged) {
            OnTimeScaleChanged -= timeScaleChanged;
            return this;
        }

        public TimeDependent ThatDoesNotProcessWhenPause() {
            ProcessWhenPause = false;
            return this;
        }

        public TimeDependent WithIgnoreGameRealTimeModifiers(bool ignore) {
            IgnoreGameRealTimeModifiers = ignore;
            return this;
        }

        protected override void OnDiscard(bool fromDomainDrop) {
            var gameRealTime = World.Any<GameRealTime>();
            if (gameRealTime != null) {
                gameRealTime.TimeScaleChanged -= RefreshTimeScale;
            }
            base.OnDiscard(fromDomainDrop);
            _timeComponents.Clear();
        }
    }
}