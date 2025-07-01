using Awaken.TG.Main.Heroes.Stats;
using Awaken.TG.Main.Utility;

namespace Awaken.TG.Main.Stories {
    class StatChangeValue {
        public Stat AffectedStat { get; private set; }
        public float Change { get; private set; }

        StatChangeValue() { }

        public static StatChangeValue Direct(Stat stat, StatValue statValue) {
            var statChange = new StatChangeValue();
            statChange.AffectedStat = stat;
            statChange.Change = statValue.GetValue(statChange.AffectedStat);
            return statChange;
        }

        [UnityEngine.Scripting.Preserve]
        public static StatChangeValue[] WithFallback(Stat stat, Stat fallbackStat, StatValue statValue) {
            var resolveChange = new StatChangeValue();
            resolveChange.AffectedStat = stat;
            resolveChange.Change = statValue.GetValue(resolveChange.AffectedStat);

            if (resolveChange.AffectedStat.ModifiedValue < -resolveChange.Change) {
                var hpChange = new StatChangeValue();
                hpChange.AffectedStat = fallbackStat;
                hpChange.Change = resolveChange.Change + resolveChange.AffectedStat.ModifiedValue;
                resolveChange.Change = -resolveChange.AffectedStat.ModifiedValue;
                return new[] { resolveChange, hpChange };
            }
            return new[] { resolveChange };
        }
    }
}