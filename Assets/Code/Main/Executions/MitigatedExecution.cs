using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using Awaken.TG.MVC;
using Awaken.Utility.Collections;
using Awaken.Utility.Maths;
using Awaken.Utility.Maths.Data;
using UnityEngine;

namespace Awaken.TG.Main.Executions {
    public class MitigatedExecution : MonoBehaviour, IService {

        const int BufferCount = 9;
        const float UrgentFrameMinThresholdPercent = 1.2f;
        const float UrgentFrameMaxThresholdPercent = 1.5f;

        Stopwatch _watch;
        FrameTiming _frameTiming = new(6);
        Buffer[] _buffers;

        static float CurrentTime => Time.realtimeSinceStartup;
        ulong CurrentFrameTicks => (ulong) _watch.ElapsedTicks;
        
        public void Init() {
            _buffers = new Buffer[BufferCount];
            for (int i = 0; i < BufferCount; i++) {
                _buffers[i] = new Buffer();
            }
            StartCoroutine(Run());
        }
        
        public void Register(Action action, IModel owner, Cost cost = Cost.Medium, Priority priority = Priority.Medium, float maxWaitTime = 1f, bool omittable = false) {
            var buffer = BufferOf(cost, priority);
            buffer.Push(new TimedAction(action, owner, 0, maxWaitTime, omittable));
        }

        public void RegisterOverTime(IList<Action> actions, float time, IModel owner, Cost cost = Cost.Medium, Priority priority = Priority.Medium, float maxWaitTime = 1f, bool omittable = false) {
            RegisterOverTime(actions, actions.Count, time, owner, cost, priority, maxWaitTime, omittable);
        }

        public void RegisterOverTime(IEnumerable<Action> actions, int count, float time, IModel owner, Cost cost = Cost.Medium, Priority priority = Priority.Medium, float maxWaitTime = 1f, bool omittable = false) {
            var buffer = BufferOf(cost, priority);
            float delta = time / count;
            float latency = 0;
            foreach (var action in actions) {
                buffer.Push(new TimedAction(action, owner, latency, maxWaitTime, omittable));
                latency += delta;
            }
        }
        
        public void RegisterOverTime(Action action, int count, float time, IModel owner, Cost cost, Priority priority = Priority.Medium, float maxWaitTime = 1f, bool omittable = false) {
            var buffer = BufferOf(cost, priority);
            float delta = time / count;
            float latency = 0;
            for (int i = 0; i < count; i++) {
                buffer.Push(new TimedAction(action, owner, latency, maxWaitTime, omittable));
                latency += delta;
            }
        }

        Buffer BufferOf(Cost cost, Priority priority) {            
            int index = priority switch {
                Priority.High => 0,
                Priority.Medium => 3,
                Priority.Low => 6,
                _ => throw new ArgumentOutOfRangeException(nameof(priority), priority, null)
            };
            index += cost switch {
                Cost.Heavy => 0,
                Cost.Medium => 1,
                Cost.Light => 2,
                _ => throw new ArgumentOutOfRangeException(nameof(cost), cost, null)
            };
            return _buffers[index];
        }
        
        IEnumerator Run() {
            _watch = new Stopwatch();

            yield return new WaitForEndOfFrame();
            _watch.Start();

            while (true) {
                yield return new WaitForEndOfFrame();

                for (int i = 0; i < BufferCount; i++) {
                    ExecuteExpiring(_buffers[i]);
                }
                for (int i = 0; i < BufferCount; i++) {
                    ExecuteUrgent(_buffers[i]);
                }
                for (int i = 0; i < BufferCount; i++) {
                    ExecuteNotUrgent(_buffers[i]);
                }

                _frameTiming.Push(CurrentFrameTicks);

                _watch.Restart();
            }
        }

        void ExecuteExpiring(Buffer buffer) {
            while (!buffer.IsEmpty && CurrentTime > buffer.Peek.MaxTime) {
                var action = buffer.Pop();
                if (!action.Omittable) {
                    action.Invoke();
                }
            }
        }

        void ExecuteUrgent(Buffer buffer) {
            while (!buffer.IsEmpty && CurrentTime > buffer.Peek.DesiredTime && CurrentFrameTicks < UrgentFrameTicksThreshold(buffer.Peek)) {
                var action = buffer.Pop();
                if (!action.Omittable || CurrentFrameTicks < _frameTiming.AverageTicks) {
                    action.Invoke();
                }
            }
        }
        float UrgentFrameTicksThreshold(TimedAction action) {
            return _frameTiming.UrgentTicksMinThreshold + _frameTiming.UrgentTicksThresholdDelta * action.InverseTimeDelta * (CurrentTime - action.DesiredTime);
        }

        void ExecuteNotUrgent(Buffer buffer) {
            while (!buffer.IsEmpty && CurrentFrameTicks < _frameTiming.AverageTicks) {
                buffer.Pop().Invoke();
            }
        }

        void OnDestroy() {
            _watch.Stop();
            _watch = null;
        }
        
        public enum Cost {
            Heavy,
            Medium,
            Light
        }

        public enum Priority {
            High,
            Medium,
            Low,
        }

        class TimedAction {
            readonly Action _action;
            readonly IModel _owner;
            public float DesiredTime { get; }
            public float MaxTime { get; }
            public float InverseTimeDelta { get; }
            public bool Omittable { get; }

            public TimedAction(Action action, IModel owner, float latency, float maxWaitTime, bool omittable) {
                _action = action;
                _owner = owner;

                float start = CurrentTime + latency;
                float halfDelay = maxWaitTime * 0.5f;
                DesiredTime = start + halfDelay;
                MaxTime = start + maxWaitTime;
                InverseTimeDelta = 1 / halfDelay;
                
                Omittable = omittable;
            }

            public void Invoke() {
                if (_owner == null || !_owner.HasBeenDiscarded) {
                    _action.Invoke();
                }
            }

            public class Comparer : IComparer<TimedAction> {
                public int Compare(TimedAction x, TimedAction y) {
                    if (ReferenceEquals(x, y)) return 0;
                    if (ReferenceEquals(null, y)) return 1;
                    if (ReferenceEquals(null, x)) return -1;
                    return x.DesiredTime.CompareTo(y.DesiredTime);
                }
            }
        }

        class Buffer {
            BinaryHeap<TimedAction> _heap = new(new TimedAction.Comparer(), 32);

            public bool IsEmpty => _heap.IsEmpty;
            public void Push(TimedAction action) => _heap.Insert(action);
            public TimedAction Peek => _heap.Peek;
            public TimedAction Pop() => _heap.Extract();
        }

        struct FrameTiming {
            FastAverageCounterUlong _frameTicks;

            public ulong UrgentTicksMinThreshold { get; private set; }
            public ulong UrgentTicksThresholdDelta { get; private set; }
            public ulong AverageTicks => _frameTicks.Average;

            public FrameTiming(int ord) : this() {
                _frameTicks = new(ord);
            }

            public void Push(ulong ticks) {
                _frameTicks.Push(ticks);
                UrgentTicksMinThreshold = (ulong) (_frameTicks.Average * UrgentFrameMinThresholdPercent);
                ulong urgentFrameTicksMaxThreshold = (ulong) (_frameTicks.Average * UrgentFrameMaxThresholdPercent);
                UrgentTicksThresholdDelta = urgentFrameTicksMaxThreshold - UrgentTicksMinThreshold;
            }
        }
    }
}