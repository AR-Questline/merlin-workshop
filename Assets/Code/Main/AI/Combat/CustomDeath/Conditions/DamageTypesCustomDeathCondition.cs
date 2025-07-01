using System;
using System.Linq;
using Awaken.TG.Main.Fights.DamageInfo;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Awaken.TG.Main.AI.Combat.CustomDeath.Conditions {
    [Serializable]
    public class DamageTypesCustomDeathCondition : ICustomDeathAnimationConditions {
        [SerializeField] bool requireSpecificDamageTypes;
        [SerializeField, ShowIf(nameof(requireSpecificDamageTypes))] DamageType[] damageTypes;
        [SerializeField] bool requireSpecificDamageSubType;
        [SerializeField, ShowIf(nameof(requireSpecificDamageSubType))] DamageSubType[] damageSubTypes;
        
        public bool Check(DamageOutcome damageOutcome, bool isValidationCheck) {
            if (requireSpecificDamageTypes) {
                if (!damageTypes.Contains(damageOutcome.Damage.Type)) {
                    return false;
                }
            }
            
            if (requireSpecificDamageSubType) {
                bool found = false;
                foreach (var subType in damageOutcome.Damage.SubTypes) {
                    if (damageSubTypes.Contains(subType.SubType)) {
                        found = true;
                        break;
                    }
                }
                if (!found) {
                    return false;
                }
            }

            return true;
        }
    }
}
