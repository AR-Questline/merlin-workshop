using System.Collections.Generic;
using Awaken.TG.Main.General.StatTypes;

namespace Awaken.TG.Main.Heroes.Stats.Observers {
    public interface IStatAndEffectProvider {
        HeroStatType HeroStat { get; }
        IEnumerable<StatEffect> Effects { get; }
    }
}