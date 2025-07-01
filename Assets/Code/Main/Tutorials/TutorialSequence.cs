using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Awaken.TG.MVC.Events;
using Awaken.Utility;
using Awaken.Utility.Debugging;
using Debug = UnityEngine.Debug;

namespace Awaken.TG.Main.Tutorials {
    /// <summary>
    /// TutorialSequence is responsible for holding and managing a queue of tutorial events.
    /// Each event is represented by WaiterSpawner. When one of them is executed, we spawn another and wait for it's execution.
    /// </summary>
    public class TutorialSequence {
        public SequenceKey Key { get; }

        readonly Dictionary<string, Func<bool>> _conditions = new();
        readonly Queue<(string key, TutorialMaster.WaiterSpawner waiterSpawner)> _queue = new();
        readonly List<TutorialMaster.WaiterSpawner> _onKill = new();

        TutorialWaiter _currentWaiter;

        public static void EDITOR_RuntimeReset() {
            Sequences.Clear();
            CreatedSequences.Clear();
        }
        
        /// <summary>
        /// It must be wrapped in <see cref="TutorialSequence.Creation"/> scope
        /// </summary>
        public static TutorialSequence Create() => new(SequenceKey.None);
        
        /// <summary>
        /// Sequence will only start if key is not consumed. <br/>
        /// Key will be consumed when Sequence ends. <br/>
        /// No matter if player completes it or it is killed by system <br/> <br/>
        /// It must be wrapped in <see cref="TutorialSequence.Creation"/> scope
        /// </summary>
        public static TutorialSequence Create(SequenceKey key) => new(key);

        TutorialSequence(SequenceKey key) {
            CheckCreationScope("constructor");
            CreatedSequences.Add(this);
            Key = key;
            PersistentConditionKeyNotConsumed(key);
        }
        
        // == Creation
        
        public TutorialSequence  PersistentConditionKeyNotConsumed(SequenceKey key) => PersistentCondition(() => !TutorialKeys.IsConsumed(key));
        public TutorialSequence PersistentCondition(Func<bool> condition) => PersistentCondition("", condition);
        public TutorialSequence PersistentCondition(string name, Func<bool> condition) {
            CheckCreationScope(nameof(PersistentCondition));
            _conditions[name] = condition;
            return this;
        }
        
        public TutorialSequence Append(TutKeys key, TutorialMaster.WaiterSpawner creator) => Append(TutorialKeys.FullKey(key), creator);
        public TutorialSequence Append(TutorialMaster.WaiterSpawner creator) => Append(TutorialKeys.Forced, creator);
        TutorialSequence Append(string key, TutorialMaster.WaiterSpawner creator) {
            CheckCreationScope(nameof(Append));
            _queue.Enqueue((key, creator));
            return this;
        }

        public TutorialSequence WaitForTrigger(TutKeys trigger, TutorialMaster master, out Reference<IEventListener> listener) {
            return Append(trigger, master.RunStep(out listener, () => TutorialKeys.Remove(trigger)));
        }

        public TutorialSequence OnKill(TutorialMaster.WaiterSpawner creator) {
            CheckCreationScope(nameof(OnKill));
            _onKill.Add(creator);
            return this;
        }

        [Conditional("DEBUG")]
        static void CheckCreationScope(string operation) {
            if (!s_isBeingCreated) {
                Log.Important?.Error($"TutorialSequence creation operation ({operation}) outside of TutorialSequence.Creation block");
            }
        }
        
        // == Execution

        void OnWaiterFinished() {
            _currentWaiter = null;
            while (_queue.Any() && _currentWaiter == null) {
                var (key, spawner) = _queue.Dequeue();
                Advance(key, spawner);
            }

            if (_currentWaiter == null) {
                OnEnd(true);
            }
        }
        
        void Advance(string key, TutorialMaster.WaiterSpawner creator) {
            if (!_conditions.Values.All(c => c())) {
                Kill(false);
                return;
            }

            
            if (TutorialMaster.DebugMode) {
                Log.Important?.Error($"TutorialSequence {Key} Advance with {key}");
            }
            
            TutorialWaiter waiter = creator?.Invoke(key);
            if (waiter is { WasPerformed: false }) {
                waiter.Callback += OnWaiterFinished;
                _currentWaiter = waiter;
            }
        }

        // == Callbacks
        
        void AfterCreation() {
            OnWaiterFinished();
        }
        
        void Kill(bool consume) {
            if (TutorialMaster.DebugMode) {
                Log.Important?.Error($"TutorialSequence {Key} Killed {string.Join(", ", _queue.Select(q => q.Item1))}");
            }
            foreach (var waiter in _onKill) {
                waiter.Invoke(TutorialKeys.Forced);
            }
            OnEnd(consume);
        }

        void OnEnd(bool consume) {
            if (TutorialMaster.DebugMode) {
                Log.Important?.Error($"TutorialSequence {Key} Ended");
            }

            Sequences.Remove(this);
            if (consume) {
                TutorialKeys.Consume(Key);
            }

            _currentWaiter = null;
            _conditions.Clear();
            _queue.Clear();
            _onKill.Clear();
        }

        // == Static Operations

        static bool s_isBeingCreated;
        public static CreationScope Creation { get {
            if (s_isBeingCreated) {
                throw new InvalidOperationException("Nested TutorialSequence.Creation block");
            }
            s_isBeingCreated = true;
            CreatedSequences.Clear();
            return new CreationScope();
        }}
        public ref struct CreationScope {
            public void Dispose() {
                s_isBeingCreated = false;
                Sequences.AddRange(CreatedSequences);
                foreach (var sequence in CreatedSequences) {
                    sequence.AfterCreation();
                }
                CreatedSequences.Clear();
            }
        }

        static readonly List<TutorialSequence> CreatedSequences = new();
        static readonly List<TutorialSequence> Sequences = new();
        public static void Kill(bool consume, params SequenceKey[] keys) {
            var sequences = Sequences.Where(sequence => keys.Contains(sequence.Key)).ToList();
            foreach (var sequence in sequences) {
                sequence.Kill(consume);
            }
        }
        public static void ClearAll() {
            Sequences.Clear();
        }
    }
}