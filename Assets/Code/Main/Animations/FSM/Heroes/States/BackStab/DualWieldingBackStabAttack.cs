using Awaken.TG.Main.Heroes.Combat;

namespace Awaken.TG.Main.Animations.FSM.Heroes.States.BackStab {
    public partial class DualWieldingBackStabAttack : BackStabAttack {
        protected override float BackStabAttackCost =>
            (ParentModel.MainHandItemStats?.LightAttackCost + ParentModel.OffHandItemStats?.LightAttackCost) *
            Hero.HeroStats.ItemStaminaCostMultiplier;
        
        protected override void PerformBackStab() {
            VHeroController vHeroController = Hero.VHeroController;
            vHeroController.BackStab(ParentModel.MainHandItem);
            vHeroController.BackStab(ParentModel.OffHandItem);
        }
    }
}