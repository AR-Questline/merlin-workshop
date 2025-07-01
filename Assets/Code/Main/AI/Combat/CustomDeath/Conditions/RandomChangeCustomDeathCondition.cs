using System;
using Awaken.TG.Code.Utility;
using Awaken.TG.Main.Fights.DamageInfo;
using UnityEngine;

namespace Awaken.TG.Main.AI.Combat.CustomDeath.Conditions {
    [Serializable]
    public class RandomChangeCustomDeathCondition : ICustomDeathAnimationConditions {
        [SerializeField] float procChance = 0.5f;
        
        public bool Check(DamageOutcome damageOutcome, bool isValidationCheck) {
            if (isValidationCheck) return true;
            return RandomUtil.WithProbability(procChance);
        }
    }
}
