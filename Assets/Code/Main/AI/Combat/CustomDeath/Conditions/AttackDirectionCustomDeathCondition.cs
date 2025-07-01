using System;
using Awaken.TG.Main.Fights.DamageInfo;
using Awaken.TG.Main.Fights.NPCs;
using Awaken.TG.Main.Grounds;
using Awaken.Utility.Maths;
using UnityEngine;

namespace Awaken.TG.Main.AI.Combat.CustomDeath.Conditions {
    [Serializable]
    public class AttackDirectionCustomDeathCondition : ICustomDeathAnimationConditions {
        [SerializeField] bool checkAngleFromAttackerToTarget;
        [SerializeField] bool checkAttackAngle;
        [SerializeField] float minAngle;
        [SerializeField] float maxAngle;

        bool IsValidAngle(float angle) {
            if (minAngle > maxAngle) return angle < maxAngle || angle > minAngle;
            return angle >= minAngle && angle <= maxAngle;
        }
        
        public bool Check(DamageOutcome damageOutcome, bool isValidationCheck) {
            var target = damageOutcome.Target;
            if (target is not NpcElement npc) return false;
            
            var forward = npc.Forward();
            if (checkAngleFromAttackerToTarget) {
                var attacker = damageOutcome.Attacker;
                if (attacker == null) return false;
                float angleFromAttacker = Vector3.SignedAngle(forward, (npc.Coords - attacker.Coords).X0Z(), Vector3.up);
                if (!IsValidAngle(angleFromAttacker)) return false;
            }

            if (checkAttackAngle) {
                if (!damageOutcome.Damage.Direction.HasValue) return false;
                float attackAngle = Vector3.SignedAngle(forward, -damageOutcome.Damage.Direction.Value.X0Z(), Vector3.up);
                if (!IsValidAngle(attackAngle)) return false;
            }

            return true;
        }
    }
}
