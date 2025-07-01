using System;
using Awaken.TG.Main.Fights.DamageInfo;
using Awaken.TG.Main.Fights.NPCs;
using Awaken.TG.Main.Heroes;
using Awaken.Utility.Extensions;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Awaken.TG.Main.AI.Combat.CustomDeath.Conditions {
    [Serializable]
    public class TargetStateCustomDeathCondition : ICustomDeathAnimationConditions {
        [SerializeField] TargetValue hasToBeStaggered;
        [SerializeField] TargetValue hasToBeUnconscious;
        [SerializeField] TargetValue hasToBeRagdolled;
        [SerializeField] bool hasToBeTheLastTarget;
        [SerializeField, ShowIf(nameof(hasToBeTheLastTarget))] bool hasToBeTheLastEnemy;
        [SerializeField] NpcState requiredState = NpcState.All;

        public bool Check(DamageOutcome damageOutcome, bool isValidationCheck) {
            if (damageOutcome.Target is not NpcElement target) {
                return false;
            }
            
            switch (hasToBeStaggered) {
                case TargetValue.CantBe when target.Staggered:
                case TargetValue.HasToBe when !target.Staggered:
                    return false;
                case TargetValue.Ignore:
                    break;
            }
            
            switch (hasToBeUnconscious) {
                case TargetValue.CantBe when target.IsUnconscious:
                case TargetValue.HasToBe when !target.IsUnconscious:
                    return false;
                case TargetValue.Ignore:
                    break;
            }

            switch (hasToBeRagdolled) {
                case TargetValue.CantBe when target.IsInRagdoll:
                case TargetValue.HasToBe when !target.IsInRagdoll:
                    return false;
                case TargetValue.Ignore:
                    break;
            }

            if (requiredState != NpcState.All) {
                var ai = target.NpcAI;
                if (!requiredState.HasFlagFast(NpcState.Idle) && ai.InIdle) {
                    return false;
                }
                if (!requiredState.HasFlagFast(NpcState.Alert) && ai.InAlert) {
                    return false;
                }
                if (!requiredState.HasFlagFast(NpcState.Combat) && ai.InCombat) {
                    return false;
                }
                if (!requiredState.HasFlagFast(NpcState.Flee) && ai.InFlee) {
                    return false;
                }
                if (!requiredState.HasFlagFast(NpcState.Returning) && (ai.InReturningToSpawn || ai.IsRunningToSpawn)) {
                    return false;
                }
            }
            if (hasToBeTheLastTarget) {
                bool isTargetValid = !hasToBeTheLastEnemy;
                foreach (var combatant in Hero.Current.PossibleAttackers) {
                    if (combatant != target) {
                        return false;
                    } else {
                        isTargetValid = true;
                    }
                }
                if (!isTargetValid) {
                    return false;
                }
            }
            return true;
        }
    }

    [Flags]
    public enum NpcState : byte {
        Idle = 1 << 1,
        Alert = 1 << 2,
        Combat = 1 << 3,
        Flee = 1 << 4,
        Returning = 1 << 5,
        
        All = Idle | Alert | Combat | Flee | Returning
    }

    public enum TargetValue : byte {
        Ignore,
        HasToBe,
        CantBe
    } 
}