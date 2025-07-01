using Awaken.TG.Main.Utility.Animations.HitStops;

namespace Awaken.TG.Main.Animations.FSM.Heroes.States.Melee {
    public partial class DualHandedHeavyAttackEnd : HeavyAttackEnd {
        protected override float HeavyAttackCost {
            get {
                if (!Hero.IsInDualHandedAttack) {
                    return base.HeavyAttackCost;
                }

                return (ParentModel.MainHandItemStats?.HeavyAttackCost + ParentModel.OffHandItemStats?.HeavyAttackCost)
                       * Hero.HeroStats.ItemStaminaCostMultiplier.ModifiedValue
                       * Hero.HeroStats.DualWieldHeavyAttackCostMultiplier;
            }
        }

        protected override HitStopData HitStopData => !Hero.IsInDualHandedAttack ? base.HitStopData : Hero.Data.hitStopsAsset.heavyAttackDualHandedData;
    }
}