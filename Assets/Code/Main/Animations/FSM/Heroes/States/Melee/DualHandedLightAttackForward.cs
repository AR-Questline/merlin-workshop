using Awaken.TG.Main.Utility.Animations.HitStops;

namespace Awaken.TG.Main.Animations.FSM.Heroes.States.Melee {
    public partial class DualHandedLightAttackForward : LightAttackForward {
        protected override float LightAttackCost {
            get {
                if (!Hero.IsInDualHandedAttack) {
                    return base.LightAttackCost;
                }

                return (ParentModel.MainHandItemStats?.LightAttackCost + ParentModel.OffHandItemStats?.LightAttackCost) 
                       * Hero.HeroStats.ItemStaminaCostMultiplier;
            }
        }

        protected override HitStopData HitStopData => !Hero.IsInDualHandedAttack ? base.HitStopData : Hero.Data.hitStopsAsset.forwardAttackDualHandedData;
    }
}