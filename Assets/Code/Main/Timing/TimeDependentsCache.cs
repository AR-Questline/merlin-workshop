using System.Collections.Generic;
using Awaken.TG.Main.Saving;
using Awaken.TG.Main.Timing.ARTime;
using Awaken.TG.MVC;
using Awaken.TG.MVC.Elements;
using Awaken.TG.MVC.Events;
using Awaken.Utility.Collections;
using Awaken.Utility.Debugging;
using UnityEngine;
using LogType = Awaken.Utility.Debugging.LogType;

namespace Awaken.TG.Main.Timing {
    public partial class TimeDependentsCache : Element<IModel> {
        public sealed override bool IsNotSaved => true;

        List<TimeDependent> _timeDependents = new List<TimeDependent>();
        List<TimeDependent> _timeDependentsToAdd = new List<TimeDependent>();
        List<TimeDependent> _timeDependentsToRemove = new List<TimeDependent>();
        bool _isProcessing;

        protected override void OnInitialize() {
            foreach (var timeDependent in World.All<TimeDependent>()) {
                Add(timeDependent);
            }
            World.EventSystem.ListenTo(EventSelector.AnySource, World.Events.ModelFullyInitialized<TimeDependent>(), this, AddModel);
            World.EventSystem.ListenTo(EventSelector.AnySource, World.Events.ModelDiscarded<TimeDependent>(), this, RemoveModel);
        }

        protected override void OnDiscard(bool fromDomainDrop) {
            _timeDependents.Clear();
            _timeDependentsToAdd.Clear();
            _timeDependentsToRemove.Clear();
        }

        public Enumerator GetEnumerator() => new(this);

        void AddModel(Model timeDependentModel) {
            Add((TimeDependent)timeDependentModel);
        }

        void Add(TimeDependent timeDependent) {
            if (_isProcessing) {
                _timeDependentsToAdd.Add(timeDependent);
            } else {
                _timeDependents.Add(timeDependent);
            }
        }

        void RemoveModel(Model timeDependentModel) {
            Remove((TimeDependent)timeDependentModel);
        }

        void Remove(TimeDependent timeDependent) {
            if (_isProcessing) {
                _timeDependentsToRemove.Add(timeDependent);
            } else {
                RemoveFromMainList(timeDependent);
            }
        }

        void Lock() {
            _isProcessing = true;
        }

        void Unlock() {
            _isProcessing = false;
            foreach (var timeDependent in _timeDependentsToAdd) {
                _timeDependents.Add(timeDependent);
            }
            _timeDependentsToAdd.Clear();
            foreach (var timeDependent in _timeDependentsToRemove) {
                RemoveFromMainList(timeDependent);
            }
            _timeDependentsToRemove.Clear();
        }

        void RemoveFromMainList(TimeDependent timeDependent) {
            var index = _timeDependents.ReverseFastIndexOf(timeDependent);
            if (index == -1) {
                Log.Important?.Error($"Trying to remove TimeDependent which is not registered: {timeDependent.ID}");
                return;
            }
            _timeDependents.RemoveAt(index);
        }

        public ref struct Enumerator {
            readonly TimeDependentsCache _cache;
            readonly List<TimeDependent> _timeDependents;
            int _index;

            public Enumerator(TimeDependentsCache cache) {
                _cache = cache;
                _index = -1;
                _timeDependents = cache._timeDependents;
                _cache.Lock();
            }

            public bool MoveNext() => ++_index < _timeDependents.Count;
            
            [UnityEngine.Scripting.Preserve] 
            public void Reset() => _index = -1;

            public TimeDependent Current => _timeDependents[_index];

            [UnityEngine.Scripting.Preserve] 
            public void Dispose() {
                _cache.Unlock();
            }
        }
    }
}
