using System;
using Awaken.TG.Main.Character;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Awaken.TG.Main.Fights.Finishers {
    [Serializable]
    public struct FinisherHealthCondition {
        [InfoBox("NPC will be killed no matter of the Health Conditions")]
        [SerializeField] Condition condition;
        [SerializeField, ShowIf(nameof(HpConditionWithValue))] float hpValue;

        bool HpConditionWithValue => condition != Condition.HasToBeKilledByDamage;
        
        public static FinisherHealthCondition Default => new FinisherHealthCondition() {
            condition = Condition.HasToBeKilledByDamage
        };

        public bool IsFulfilled(float predictedDamage, float npcHp, IAlive target) {
            switch (condition) {
                case Condition.HasToBeKilledByDamage when predictedDamage < npcHp:
                case Condition.CanBeLeftWithXOrLessHPPercentage when predictedDamage + target.MaxHealth.ModifiedValue * hpValue / 100f < npcHp:
                case Condition.CanBeLeftWithXOrLessHP when predictedDamage + hpValue < npcHp:
                case Condition.HasToBeBelowXHP when hpValue >= npcHp:
                case Condition.HasToBeBelowXHPPercentage when hpValue >= target.Health.Percentage:
                    return false;
            }
            return true;
        }
        
        public enum Condition : byte {
            HasToBeKilledByDamage,
            CanBeLeftWithXOrLessHP,
            CanBeLeftWithXOrLessHPPercentage,
            HasToBeBelowXHP,
            HasToBeBelowXHPPercentage,
        }
    }
}
