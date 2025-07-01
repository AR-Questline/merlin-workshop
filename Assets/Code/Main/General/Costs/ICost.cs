using Awaken.TG.Main.Heroes.Stats;

namespace Awaken.TG.Main.General.Costs {
    public interface ICost {
        bool CanAfford();
        void Pay();
        void Refund();
        
        bool TryStack(ICost cost);
        ICost Clone();

        float CombinedStatCost(StatType statType);
    }
}