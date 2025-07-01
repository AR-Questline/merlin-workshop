using System;
using Awaken.TG.Main.Fights.DamageInfo;
using UnityEngine;

namespace Awaken.TG.Main.AI.Combat.CustomDeath.Conditions {
    [Serializable]
    public class AttackCustomDeathCondition : ICustomDeathAnimationConditions {
        [SerializeField] bool hasToBeHeavyAttack;
        [SerializeField] bool hasToBeBackstab;

        public bool Check(DamageOutcome damageOutcome, bool isValidationCheck) {
            if (hasToBeHeavyAttack && !damageOutcome.Damage.IsHeavyAttack) {
                return false;
            }
            if (hasToBeBackstab && !damageOutcome.DamageModifiersInfo.IsBackStab) {
                return false;
            }
            return true;
        }
    }
}
