using Awaken.TG.Main.Animations.FSM.Heroes.Machines;
using Awaken.TG.Main.Animations.FSM.Heroes.States.Shared;
using Awaken.TG.Main.Heroes.Combat;

namespace Awaken.TG.Main.Animations.FSM.Heroes.States.Bow {
    public partial class BowEquipWeapon : EquipWeaponBase<BowFSM> {
        CharacterBow _bow;

        protected override void AfterEnter(float previousStateNormalizedTime) {
            if (ParentModel.HeroBow != null) {
                ParentModel.HeroBow.OnEquipBow();
            }
            base.AfterEnter(previousStateNormalizedTime);
        }
        
        protected override void OnExit(bool restarted) {
            if (ParentModel.HeroBow != null) {
                ParentModel.HeroBow.OnBowIdle();
                ParentModel.HeroBow.ToggleArrows(false);
            }
        }
    }
}