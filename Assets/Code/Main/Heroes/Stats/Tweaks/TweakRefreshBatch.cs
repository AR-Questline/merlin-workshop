using System.Collections.Generic;
using Awaken.TG.MVC;
using Awaken.Utility.Collections;

namespace Awaken.TG.Main.Heroes.Stats.Tweaks {
    /// <summary>
    /// Collects all modified stats and models to recalculate and trigger them only once in single batch
    /// <remarks> Should be obtain by <code>GenericPool&lt;TweakRefreshBatch&gt;.Get()</code></remarks>
    /// <remarks> You must clear the instance before you call <code>GenericPool&lt;TweakRefreshBatch&gt;.Release(refreshBatch)</code></remarks>
    /// </summary>
    public class TweakRefreshBatch {
        // Preallocate collections as it will be reused
        const int PreallocateSize = 32;
        readonly HashSet<Stat> _statsToRecalculate = new HashSet<Stat>(PreallocateSize);
        readonly HashSet<IModel> _modelsToTrigger = new HashSet<IModel>(PreallocateSize);

        public void Add(Stat stat) {
            _statsToRecalculate.Add(stat);
            _modelsToTrigger.Add(stat.Owner);
        }

        [UnityEngine.Scripting.Preserve]
        public void Add(IModel model) {
            _modelsToTrigger.Add(model);
        }

        public void Trigger() {
            _statsToRecalculate.ForEach(stat => stat.RecalculateTweaks(false));
            _statsToRecalculate.ForEach(stat => stat.TriggerStatChanged());
            _modelsToTrigger.ForEach(m => m.TriggerChange());

            Clear();
        }

        public void Clear() {
            _statsToRecalculate.Clear();
            _modelsToTrigger.Clear();
        }
    }
}