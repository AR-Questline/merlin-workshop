using System;
using Awaken.TG.Main.Fights;
using Awaken.TG.Main.Grounds;
using Awaken.TG.Main.Heroes.Combat;
using Awaken.TG.Utility;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Awaken.TG.Main.AI.Combat.Behaviours.Abstracts {
    [Serializable]
    public abstract partial class CombatEnemyBehaviourBase : EnemyBehaviourBase, IBehaviourBase {
        const float MinValueOfMaxDistance = VHeroCombatSlots.FirstLineCombatSlotOffset + 0.5f;
        protected const string InvokeParametersGroup = "Invoke Parameters";
        protected const string KnockdownPropertiesGroup = "Hero Knockdown Properties";
        
        [BoxGroup(BasePropertiesGroup), PropertyOrder(-3)]
        [SerializeField] int specialAttackIndex;
        [BoxGroup(BasePropertiesGroup), PropertyOrder(-2), Range(0, 999)]
        [SerializeField] int weight = 10;
        [BoxGroup(BasePropertiesGroup), PropertyOrder(-1)] 
        [SerializeField] CombatBehaviourCooldown cooldown = CombatBehaviourCooldown.None;
        [BoxGroup(BasePropertiesGroup), PropertyOrder(-1), ShowIf(nameof(ShowCooldownDuration)), LabelText("Duration"), Range(0.1f, 999f), Indent(1)]
        [SerializeField] float cooldownDuration = 1f;
        [BoxGroup(InvokeParametersGroup)] 
        [SerializeField] bool acceptOnlyAtAngle;
        [BoxGroup(InvokeParametersGroup), ShowIf(nameof(acceptOnlyAtAngle)), LabelText("Minimum"), Indent(1)]
        [SerializeField] float minAngleToTarget = -45f;
        [BoxGroup(InvokeParametersGroup), ShowIf(nameof(acceptOnlyAtAngle)), LabelText("Maximum"), Indent(1)]
        [SerializeField] float maxAngleToTarget = 45f;
        [BoxGroup(InvokeParametersGroup), Range(0, 50f)]
        [SerializeField] float minDistance;
        [BoxGroup(InvokeParametersGroup), Range(MinValueOfMaxDistance, 999f)]
        [SerializeField] float maxDistance = 3.5f;

        [BoxGroup(KnockdownPropertiesGroup), PropertyOrder(0)] 
        [SerializeField] KnockdownType knockdownType;
        [BoxGroup(KnockdownPropertiesGroup), PropertyOrder(0), ShowIf(nameof(ShowKnockdownStrength))] 
        [SerializeField] float knockdownStrength;
        
        public override int Weight => weight;
        public override bool IsPeaceful => false;
        public override bool RequiresCombatSlot => false;
        public override int SpecialAttackIndex => specialAttackIndex;
        public abstract bool CanBeUsed { get; }
        public KnockdownType KnockdownType => knockdownType;
        public virtual float KnockdownStrength => knockdownStrength;
        protected override CombatBehaviourCooldown Cooldown => cooldown;
        protected override float CooldownDuration => cooldownDuration;
        protected virtual bool IgnoreBaseConditions => false;
        
        public virtual float MinDistance => minDistance;
        public virtual float MaxDistance => maxDistance;
        public bool InRange => (IgnoreBaseConditions || ParentModel.DistanceToTarget >= MinDistance) && ParentModel.DistanceToTarget <= MaxDistance;

        public override bool UseConditionsEnsured() {
            var target = ParentModel.NpcElement.GetCurrentTarget();
            if (target == null) {
                return false;
            }

            if (!InRange) {
                return false;
            }

            if (!IgnoreBaseConditions && acceptOnlyAtAngle) {
                var npcForward = ParentModel.NpcElement.Forward().ToHorizontal3();
                var dirToTarget = (target.Coords - ParentModel.Coords).ToHorizontal3();
                float angleToTarget = Vector3.SignedAngle(npcForward, dirToTarget, Vector3.up);
                bool validAngle = angleToTarget >= minAngleToTarget && angleToTarget <= maxAngleToTarget;
                if (!validAngle) {
                    return false;
                }
            }

            return CanBeUsed;
        }
        
        // Editor
        bool ShowCooldownDuration => cooldown == CombatBehaviourCooldown.UntilTimeElapsed;
        bool ShowKnockdownStrength => knockdownType != KnockdownType.None;
    }

    public enum KnockdownType : byte {
        None = 0,
        OnDamageTaken = 1,
        Always = 2
    }
}