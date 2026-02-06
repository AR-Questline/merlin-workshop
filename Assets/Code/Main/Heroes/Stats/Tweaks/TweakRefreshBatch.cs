using System;
using System.Collections.Generic;
using Awaken.TG.MVC;

namespace Awaken.TG.Main.Heroes.Stats.Tweaks {
    /// <summary>
    /// Collects all modified stats and models to recalculate and trigger them only once in single batch
    /// <remarks> Should be obtain by <code>GenericPool&lt;TweakRefreshBatch&gt;.Get()</code></remarks>
    /// <remarks> You must clear the instance before you call <code>GenericPool&lt;TweakRefreshBatch&gt;.Release(refreshBatch)</code></remarks>
    /// </summary>
    public class TweakRefreshBatch {
        // Preallocate collections as it will be reused
        const int PreallocateSize = 32;
        
        readonly HashSet<Stat> _statsToRecalculate = new(PreallocateSize);
        readonly HashSet<IModel> _modelsToTrigger = new(PreallocateSize);

        readonly HashSet<StatToRecalculate> _statsToRecalculateData = new(PreallocateSize);
        
        public void Add(Stat stat) {
            _statsToRecalculate.Add(stat);
            _modelsToTrigger.Add(stat.Owner);
        }

        [UnityEngine.Scripting.Preserve]
        public void Add(IModel model) {
            _modelsToTrigger.Add(model);
        }

        public void Trigger() {
            foreach (Stat stat in _statsToRecalculate) {
                var newValue = stat.RecalculateTweaks(out float previousValue, false);
                _statsToRecalculateData.Add(new StatToRecalculate(stat, newValue - previousValue));
            }
            foreach (Stat stat in _statsToRecalculate) {
                stat.TriggerStatChanged();
            }
            foreach (IModel m in _modelsToTrigger) {
                m.TriggerChange();
            }
            foreach (StatToRecalculate toRecalculate in _statsToRecalculateData) {
                toRecalculate.stat.TryTriggerStatChangedBy(toRecalculate.valueChange);
            }
            Clear();
        }

        void Clear() {
            _statsToRecalculate.Clear();
            _modelsToTrigger.Clear();
            _statsToRecalculateData.Clear();
        }

        readonly struct StatToRecalculate : IEquatable<StatToRecalculate> {
            public readonly Stat stat;
            public readonly float valueChange;

            public StatToRecalculate(Stat stat, float valueChange) {
                this.stat = stat;
                this.valueChange = valueChange;
            }

            public bool Equals(StatToRecalculate other) {
                return Equals(stat, other.stat);
            }

            public override bool Equals(object obj) {
                return obj is StatToRecalculate other && Equals(other);
            }

            public override int GetHashCode() {
                return (stat != null ? stat.GetHashCode() : 0);
            }
        }
    }
}