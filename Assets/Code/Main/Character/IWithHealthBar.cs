using Awaken.TG.Main.Heroes.Stats;
using Awaken.TG.MVC;

namespace Awaken.TG.Main.Character {
    public interface IWithHealthBar : IModel {
        LimitedStat HealthStat { get; }
    }
}