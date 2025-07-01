using Awaken.TG.Main.Heroes.Stats;

namespace Awaken.TG.Main.General.Costs {
    public class NoCost : ICost {
        public static NoCost Instance = new();
        
        NoCost() { }
        
        public bool CanAfford() => true;

        public void Pay() { }
        public void Refund() { }

        public bool TryStack(ICost cost) => false;

        public ICost Clone() => Instance;

        public float CombinedStatCost(StatType statType) => 0;
    }
}