using System.ComponentModel;
using Awaken.TG.Main.Character;
using Awaken.TG.Main.Grounds;
using Awaken.TG.Main.Heroes.Items;
using UnityEngine;

namespace Awaken.TG.Main.Fights.DamageInfo {
    public struct AttackParameters {
        public ICharacter ICharacter { get; set; }
        public Item Item { get; set; }
        public DamageTypeData TypeData { [UnityEngine.Scripting.Preserve] get; set; }
        public float ArmorPenetration { [UnityEngine.Scripting.Preserve] get; set; }
        public AttackType AttackType { get; set; }
        public Vector3 AttackDirection { get; set; }
        
        // --- Physical Parameters
        [Description("Amount of Force damage that will be applied to target. If target ForceStumbleThreshold reaches max value it will enter Stumble.")]
        public float ForceDamage { get; set; }
        [Description("Force magnitude that will be applied if target will enter ragdoll")]
        public float RagdollForce { get; set; }
        [Description("Amount of Poise that will be applied to target.")]
        public float PoiseDamage { get; set; }
        
        public bool IsHeavyAttack => AttackType is AttackType.Heavy;
        public bool IsDashAttack => AttackType is AttackType.Lunge;
        public bool IsPush => AttackType is AttackType.Pommel;

        public AttackParameters(ICharacter character, Item item, AttackType attackType, Vector3? attackDirection) {
            ICharacter = character;
            
            Item = item;
            TypeData = item.ItemStats.DamageTypeData;
            ArmorPenetration = item.ItemStats.ArmorPenetration;

            AttackType = attackType;
            
            ForceDamage = item.ItemStats.ForceDamage;
            RagdollForce = item.ItemStats.RagdollForce;
            
            PoiseDamage = item.ItemStats.PoiseDamage;
            if (attackType is AttackType.Heavy) {
                PoiseDamage *= item.ItemStats.PoiseDamageHeavyAttackMultiplier;
            }

            AttackDirection = attackDirection ?? character.Forward();
        }
    }
}