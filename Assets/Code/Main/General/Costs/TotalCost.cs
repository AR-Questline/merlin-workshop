using System.Collections.Generic;
using System.Linq;
using Awaken.TG.Main.Heroes.Stats;

namespace Awaken.TG.Main.General.Costs {
    /// <summary>
    /// Model of cost that joins any other cost into one
    /// </summary>
    public class TotalCost : ICost {
        List<ICost> _costs;

        public TotalCost(IEnumerable<ICost> costs) {
            _costs = new List<ICost>();
            foreach (var cost in costs) {
                TryStack(cost);
            }
        }

        public bool CanAfford() {
            return _costs.All(c => c.CanAfford());
        }

        public void Pay() {
            _costs.ForEach(c => c.Pay());
        }

        public void Refund() {
            _costs.ForEach(c => c.Refund());
        }

        public bool TryStack(ICost cost) {
            if (cost is TotalCost totalCost) {
                foreach (var c in totalCost._costs) {
                    TryStack(c);
                }
            } else {
                if (_costs.All(c => !c.TryStack(cost))) {
                    _costs.Add(cost.Clone());
                }
            }
            return true;
        }

        public ICost Clone() {
            return new TotalCost(_costs);
        }

        public override string ToString() {
            return string.Join(", ", _costs.Select(c => c.ToString()));
        }

        public float CombinedStatCost(StatType statType) {
            return _costs.OfType<StatCost>().Where(sc => sc.Stat.Type == statType).Sum(sc => sc.Amount);
        }
    }
}