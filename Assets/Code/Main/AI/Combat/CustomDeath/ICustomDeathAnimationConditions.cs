using Awaken.TG.Main.Fights.DamageInfo;

namespace Awaken.TG.Main.AI.Combat.CustomDeath {
    public interface ICustomDeathAnimationConditions {
        bool Check(DamageOutcome damageOutcome, bool isValidationCheck);
    }
}
