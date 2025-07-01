using System.Collections.Generic;
using System.Linq;
using Awaken.Utility;
using Awaken.Utility.Collections;

namespace Awaken.TG.Main.Heroes.Stats {
    /// <summary>
    /// Special stat used to calculate tweaks of multiple stats.
    /// Shouldn't be saved, this is only for calculation purposes.
    /// </summary>
    public sealed partial class CompoundStat : Stat {
        public override ushort TypeForSerialization => SavedTypes.CompoundStat;

        bool _isMultiplicative;
        readonly List<Stat> _stats;
        
        public IEnumerable<Stat> Stats => _stats;
        public override StatType Type => _stats.FirstOrDefault()?.Type;
        public override float BaseValue => _isMultiplicative 
            ? 1f + _stats.Sum(s => s.BaseValue - 1f) 
            : _stats.Sum(s => s.BaseValue);
        public override float ModifiedValue => RecalculateTweaks(false);

        public CompoundStat(bool isMultiplicative, params Stat[] stats) {
            _isMultiplicative = isMultiplicative;
            _stats = stats.WhereNotNull().ToList();
        }
    }
}