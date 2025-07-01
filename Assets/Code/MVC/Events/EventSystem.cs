using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using Awaken.TG.Main.Utility.Debugging;
using Awaken.Utility.Collections;
using Awaken.Utility.Debugging;
using Awaken.Utility.Profiling;
using Awaken.Utility.Threads;
using JetBrains.Annotations;
using Unity.Collections;

namespace Awaken.TG.MVC.Events {
    public sealed class EventSystem : IService {
        const int ByOwnerPrealloc = 25_000;
        const int BySelectorPrealloc = 49_500;
        const int ByTargetPrealloc = 14_500;

        // === Fields

        readonly Dictionary<string, StructList<IEventListener>> _byTarget = new(ByOwnerPrealloc);
        readonly Dictionary<EventSelector, StructList<IEventListener>> _bySelector = new(BySelectorPrealloc);
        readonly Dictionary<IListenerOwner, StructList<IEventListener>> _byOwner = new(ByTargetPrealloc);
        readonly Queue<TriggerData> _queuedEvents = new();

        readonly List<IEventListener> _removedListenersCache = new List<IEventListener>(16);

        // === Properties
        QueuingHandle QueueHandle { get; set; }
        bool QueuingEnabled => QueueHandle != null;

        // === Public interface
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static string PatternForModel(IModel model) => model.ID;

        public IEventListener ListenTo<TSource, TPayload>(string targetPattern, IEvent<TSource, TPayload> evt, [NotNull] IListenerOwner owner, Action<TPayload> callback) {
            ThreadSafeUtils.AssertMainThread();
            var selector = new EventSelector(targetPattern, evt);
            var listener = new EventListener<TPayload>(callback, owner, selector);
            AddListener(selector, listener, true);
            return listener;
        }

        public IEventListener ListenTo<TSource, TPayload>(string targetPattern, IEvent<TSource, TPayload> evt, [NotNull] IListenerOwner owner, Action callback) =>
            ListenTo(targetPattern, evt, owner, _ => callback());
        
        public IEventListener LimitedListenTo<TSource, TPayload>(string targetPattern, IEvent<TSource, TPayload> evt, [NotNull] IListenerOwner owner, Action<TPayload> callback, int charges) {
            ThreadSafeUtils.AssertMainThread();
            var selector = new EventSelector(targetPattern, evt);
            var listener = new LimitedEventListener<TPayload>(callback, owner, selector, charges);
            AddListener(selector, listener, true);
            return listener;
        }

        public IEventListener ModalListenTo<TSource, TPayload>(string targetPattern, IEvent<TSource, TPayload> evt, [NotNull] IListenerOwner owner, Action<TPayload> callback) {
            ThreadSafeUtils.AssertMainThread();
            var selector = new EventSelector(targetPattern, evt);
            var listener = new EventListener<TPayload>(callback, owner, selector, true);
            AddListener(selector, listener, true);
            return listener;
        }

        [Pure]
        public IEventListener ListenTo<TSource, TPayload>(string targetPattern, IEvent<TSource, TPayload> evt, Action<TPayload> callback) {
            ThreadSafeUtils.AssertMainThread();
            var selector = new EventSelector(targetPattern, evt);
            var listener = new EventListener<TPayload>(callback, null, selector);
            AddListener(selector, listener, false);
            return listener;
        }

        [Pure]
        public IEventListener ListenTo<TSource, TPayload>(string targetPattern, IEvent<TSource, TPayload> evt, Action callback) {
            return ListenTo(targetPattern, evt, _ => callback());
        }

        [Pure]
        public IEventListener LimitedListenTo<TSource, TPayload>(string targetPattern, IEvent<TSource, TPayload> evt, Action<TPayload> callback, int charges) {
            ThreadSafeUtils.AssertMainThread();
            var selector = new EventSelector(targetPattern, evt);
            var listener = new LimitedEventListener<TPayload>(callback, null, selector, charges);
            AddListener(selector, listener, false);
            return listener;
        }
        
        [Pure] [UnityEngine.Scripting.Preserve]
        public IEventListener ModalListenTo<TSource, TPayload>(string targetPattern, IEvent<TSource, TPayload> evt, Action<TPayload> callback) {
            ThreadSafeUtils.AssertMainThread();
            var selector = new EventSelector(targetPattern, evt);
            var listener = new EventListener<TPayload>(callback, null, selector, true);
            AddListener(selector, listener, false);
            return listener;
        }

        void AddListener(EventSelector selector, IEventListener listener, bool checkListener) {
            ThreadSafeUtils.AssertMainThread();
            if (listener.Owner != null) {
                AddListenerToGeneric(_byOwner, listener.Owner, listener);
            } else {
#if UNITY_EDITOR
                if (checkListener) {
                    Log.Debug?.Warning("Adding listener without owner!");
                }
#endif
            }

            AddListenerToGeneric(_byTarget, selector.TargetPattern, listener);
            AddListenerToGeneric(_bySelector, selector, listener);

            ProfilerValues.EventsListenersCounters.Add();
        }

        static void AddListenerToGeneric<T>(Dictionary<T, StructList<IEventListener>> dictionary, T key, IEventListener listener) {
            if (!dictionary.TryGetValue(key, out var listeners)) {
                listeners = new StructList<IEventListener>(1);
            }
#if DEBUG || AR_DEBUG
            if (!listeners.AddUnique(listener)) {
                Log.Critical?.Error("Listener already added!");
            }
#else
            listeners.Add(listener);
#endif
            dictionary[key] = listeners;
        }
        
        // === Triggering events

        public void Trigger<TSource, TPayload>(IEventSource source, IEvent<TSource, TPayload> evt, TPayload payload) {
            if (QueuingEnabled && evt.CanBeQueued) {
                _queuedEvents.Enqueue(new TriggerData(source, evt, payload));
            } else {
                InvokeEvent(source, evt, payload);
            }
        }
        
        [UnityEngine.Scripting.Preserve]
        public void Trigger(IEventSource source, IEvent evt) {
            if (QueuingEnabled && evt.CanBeQueued) {
                _queuedEvents.Enqueue(new TriggerData(source, evt, null));
            } else {
                InvokeEvent(source, evt, null);
            }
        }
        
        void InvokeEvent(IEventSource source, IEvent evt, object payload) {
            ProfilerValues.EventsCalled.Value += 1;
            
            // selector for "only from selected source" listeners
            var namedSourceEventSelector = new EventSelector(source.ID, evt);
            InvokeEvent(source, evt, payload, namedSourceEventSelector);
            // selector for "all objects" listeners
            var anySourceEventSelector = new EventSelector(EventSelector.AnySource, evt);
            InvokeEvent(source, evt, payload, anySourceEventSelector);
        }
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        void InvokeEvent(IEventSource source, IEvent evt, object payload, EventSelector eventSelector) {
            ProfilerValues.EventsSelectorsCalled.Value += 1;
            if (_bySelector.TryGetValue(eventSelector, out var listeners)) {
                using var stableListeners = RentedArray<IEventListener>.Borrow(listeners);
                foreach (IEventListener listener in stableListeners) {
                    if (listener.Owner?.CanReceiveEvents == false) {
                        continue;
                    }
                    ProfilerValues.EventsCallbacksCount.Value += 1;
                    try {
                        IncreaseCallsDepth();
                        listener.InvokeWith(payload);
                        if (listener is IDisposableEventListener { ShouldBeDisposed: true }) {
                            RemoveListener(listener);
                        }
                    } catch (Exception e) {
                        LogUtils.LogEventException(e, evt, source, listener);
                    } finally {
                        DecreaseCallsDepth();
                    }
                }
            }
        }

        // === Removing listeners

        public void RemoveListener(IEventListener listener, bool removeModals = false) {
            ThreadSafeUtils.AssertMainThread();
            if (!removeModals && listener.IsModal) return;

            if (listener.Owner != null) RemoveFromGeneric(_byOwner, listener.Owner, listener);
            RemoveFromGeneric(_byTarget, listener.Selector.TargetPattern, listener);
            RemoveFromGeneric(_bySelector, listener.Selector, listener);

            ProfilerValues.EventsListenersCounters.Remove();
        }

        public void DisposeListener(ref IEventListener listener) {
            RemoveListener(listener, true);
            listener = null;
        }
        
        public void TryDisposeListener(ref IEventListener listener) {
            if (listener != null) {
                RemoveListener(listener, true);
                listener = null;
            }
        }

        public void RemoveAllListenersTiedTo(IEventSource source, bool removeModals = false) {
            ThreadSafeUtils.AssertMainThread();
            string id = source.ID;

            if (!_byTarget.TryGetValue(id, out var byTargetListeners)) {
                return;
            }
            _removedListenersCache.Clear();

            foreach (IEventListener l in byTargetListeners) {
                if (!removeModals && l.IsModal) continue;
                if (l.Owner != null) RemoveFromGeneric(_byOwner, l.Owner, l);
                RemoveFromGeneric(_bySelector, l.Selector, l);
                _removedListenersCache.Add(l);
            }

            RemoveFromGeneric(_byTarget, id, _removedListenersCache);
            ProfilerValues.EventsListenersCounters.Remove(_removedListenersCache.Count);

            _removedListenersCache.Clear();
        }

        public void RemoveAllListenersOwnedBy(IListenerOwner owner, bool removeModals = false) {
            ThreadSafeUtils.AssertMainThread();

            if (!_byOwner.TryGetValue(owner, out var byOwnerListeners)) {
                return;
            }

            _removedListenersCache.Clear();
            foreach (IEventListener l in byOwnerListeners) {
                if (!removeModals && l.IsModal) continue;
                RemoveFromGeneric(_byTarget, l.Selector.TargetPattern, l);
                RemoveFromGeneric(_bySelector, l.Selector, l);
                _removedListenersCache.Add(l);
            }

            RemoveFromGeneric(_byOwner, owner, _removedListenersCache);
            ProfilerValues.EventsListenersCounters.Remove(_removedListenersCache.Count);

            _removedListenersCache.Clear();
        }

        public void RemoveListenersForEventOwnedBy(IListenerOwner owner, IEvent evt, bool removeModals = false) {
            ThreadSafeUtils.AssertMainThread();

            if (!_byOwner.TryGetValue(owner, out var byOwnerListeners)) {
                return;
            }

            _removedListenersCache.Clear();
            foreach (IEventListener l in byOwnerListeners) {
                if (!removeModals && l.IsModal) continue;
                if (!ReferenceEquals(l.Selector.Event, evt)) continue;
                RemoveFromGeneric(_byTarget, l.Selector.TargetPattern, l);
                RemoveFromGeneric(_bySelector, l.Selector, l);
                _removedListenersCache.Add(l);
            }

            RemoveFromGeneric(_byOwner, owner, _removedListenersCache);
            ProfilerValues.EventsListenersCounters.Remove(_removedListenersCache.Count);
            _removedListenersCache.Clear();
        }

        public void RemoveAllListenersBetween(IEventSource source, IListenerOwner owner, bool removeModals = false) {
            ThreadSafeUtils.AssertMainThread();
            string id = source.ID;

            if (!_byTarget.TryGetValue(id, out var byTargetListeners)) {
                return;
            }

            _removedListenersCache.Clear();
            foreach (IEventListener l in byTargetListeners) {
                if (!removeModals && l.IsModal) {
                    continue;
                }
                if (l.Owner == owner) {
                    _removedListenersCache.Add(l);
                }
            }

            foreach (var listener in _removedListenersCache) {
                RemoveListener(listener, removeModals);
            }
            _removedListenersCache.Clear();
        }

        static void RemoveFromGeneric<T>(Dictionary<T, StructList<IEventListener>> dictionary, T key, IEventListener listener) {
            if (dictionary.TryGetValue(key, out var listeners)) {
                if (listeners.Remove(listener)) {
                    if (listeners.Count == 0) {
                        dictionary.Remove(key);
                    } else {
                        dictionary[key] = listeners;
                    }
                }
            }
        }

        static void RemoveFromGeneric<T>(Dictionary<T, StructList<IEventListener>> dictionary, T key, List<IEventListener> toRemoves) {
            if (dictionary.TryGetValue(key, out var listeners)) {
                var changed = false;
                foreach (var listener in toRemoves) {
                    changed = listeners.Remove(listener) | changed;
                }
                if (changed) {
                    if (listeners.Count == 0) {
                        dictionary.Remove(key);
                    } else {
                        dictionary[key] = listeners;
                    }
                }
            }
        }
        
        // === Queue Operations
        public QueuingHandle EventsQueuing() {
            // Use disposable pattern here, to ensure that TriggerQueuedEvents will happen, even if exceptions are thrown in between
            return new QueuingHandle(this);
        }
        
        void TriggerQueuedEvents() {
            while (_queuedEvents.Any()) {
                TriggerData data = _queuedEvents.Dequeue();
                InvokeEvent(data.Source, data.Event, data.Payload);
            }
        }

        // === Other
        public void PreAllocateMyListeners(IListenerOwner owner, int count) {
            PreAllocate(_byOwner, owner, count);
        }

        // === Helper
        void PreAllocate<T>(Dictionary<T, StructList<IEventListener>> dictionary, T key, int count) {
            var listeners = new StructList<IEventListener>(count);
            dictionary.TryAdd(key, listeners);
        }

        public class QueuingHandle : IDisposable {
            EventSystem _system;
        
            public QueuingHandle(EventSystem system) {
                _system = system;
                _system.QueueHandle = this;
            }
        
            public void Dispose() {
                _system.QueueHandle = null;
                _system.TriggerQueuedEvents();
            }
        }
        
        // === Debug
        int _callsDepth;
        [Conditional("DEBUG")]
        void IncreaseCallsDepth() {
            _callsDepth++;
            var currentMax = ProfilerValues.EventsCallsMaxDepth.Value;
            ProfilerValues.EventsCallsMaxDepth.Value = Math.Max(currentMax, _callsDepth);
        }
        
        [Conditional("DEBUG")]
        void DecreaseCallsDepth() {
            _callsDepth--;
        }

#if UNITY_EDITOR
        [UnityEditor.MenuItem("TG/Optimization/Log events main counts")]
        static void LogEventsMainCounts() {
            var byOwner = World.EventSystem._byOwner;
            UnityEngine.Debug.Log($"ByOwner count: {byOwner.Count} - Median({GetMedian(byOwner.Values)})");
            var bySelector = World.EventSystem._bySelector;
            UnityEngine.Debug.Log($"BySelector count: {bySelector.Count} - Median({GetMedian(bySelector.Values)})");
            var byTarget = World.EventSystem._byTarget;
            UnityEngine.Debug.Log($"ByTarget count: {byTarget.Count} - Median({GetMedian(byTarget.Values)})");

            static float GetMedian<T>(ICollection<StructList<T>> collections) {
                const int maxCount = 256;

                var lengths = new NativeList<int>(16, ARAlloc.Temp);
                foreach (var collection in collections) {
                    if (collection.Count > maxCount) {
                        continue;
                    }
                    lengths.Add(collection.Count);
                }

                if (lengths.Length == 0) {
                    return 0;
                }

                lengths.Sort();
                int mid = lengths.Length / 2;
                float median = lengths.Length % 2 == 0 ? (lengths[mid - 1] + lengths[mid]) / 2f : lengths[mid];
                lengths.Dispose();
                return median;
            }
        }

        [UnityEditor.MenuItem("TG/Optimization/Log events by target counts")]
        static void LogEventsByTarget() {
            var byTarget = World.EventSystem._byTarget;
            UnityEngine.Debug.Log($"ByTarget count: {byTarget.Count}");

            foreach (var (target, values) in byTarget.OrderByDescending(p => p.Value.Count)) {
                UnityEngine.Debug.Log($"For target: {target} there are {values.Count}/{values.Capacity} listeners");
            }
        }

        [UnityEditor.MenuItem("TG/Optimization/Log events by selector counts")]
        static void LogEventsBySelector() {
            var bySelector = World.EventSystem._bySelector;
            UnityEngine.Debug.Log($"BySelector count: {bySelector.Count}");

            foreach (var (selector, values) in bySelector.OrderByDescending(p => p.Value.Count)) {
                UnityEngine.Debug.Log($"For selector: {selector} there are {values.Count}/{values.Capacity} listeners");
            }
        }

        [UnityEditor.MenuItem("TG/Optimization/Log events by owner counts")]
        static void LogEventsByOwner() {
            var byOwner = World.EventSystem._byOwner;
            UnityEngine.Debug.Log($"ByOwner count: {byOwner.Count}");

            foreach (var (owner, values) in byOwner.OrderByDescending(p => p.Value.Count)) {
                UnityEngine.Debug.Log($"For owner: {owner} there are {values.Count}/{values.Capacity} listeners");
            }
        }
#endif
    }
}
