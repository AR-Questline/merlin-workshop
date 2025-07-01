using Awaken.TG.MVC;

namespace Awaken.TG.Main.Heroes.Stats {
    public interface IWithStats : IModel {
        Stat Stat(StatType statType);
    }
}
