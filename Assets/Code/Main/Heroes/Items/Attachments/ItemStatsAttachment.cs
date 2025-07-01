using System;
using System.Collections.Generic;
using System.Linq;
using Awaken.TG.Main.Fights.DamageInfo;
using Awaken.TG.Main.Heroes.Items.Weapons;
using Awaken.TG.Main.Locations.Attachments;
using Awaken.TG.Main.Templates;
using Awaken.TG.Main.Templates.Attachments;
using Awaken.TG.MVC.Elements;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Awaken.TG.Main.Heroes.Items.Attachments {
    [AttachesTo(typeof(ItemTemplate), AttachmentCategory.Common, "Used to define stats for equippable items.")]
    [RequireComponent(typeof(ItemTemplate))]
    public class ItemStatsAttachment : MonoBehaviour, IAttachmentSpec {
        // --- Stamina Costs
        [FoldoutGroup("Stamina Costs")]
        [VerticalGroup("Stamina Costs/IsWeapon", VisibleIf = nameof(IsWeapon))]
        public float lightAttackStaminaCost = 10f,
            heavyAttackStaminaCost = 25f,
            heavyAttackHoldCostPerTick = 2f,
            blockStaminaCostMultiplier = 0.5f, 
            parryStaminaCost = 25f;
        
        [FoldoutGroup("Stamina Costs"), ShowIf(nameof(IsRanged))] public float drawBowStaminaCostPerTick = 20f;
        [FoldoutGroup("Stamina Costs"), ShowIf(nameof(IsRangedOrShield))] public float holdItemStaminaCostPerTick = 10f;
        [FoldoutGroup("Stamina Costs"), ShowIf(nameof(IsMelee))] public float pushStaminaCost = 10f;

        
        [CustomValueDrawer("@Awaken.TG.EditorOnly.OdinHelpers.Space(10)"), ShowInInspector] string _space_ODIN1;
        // --- Damage
        [FoldoutGroup("Damage", VisibleIf = nameof(IsWeapon))]
        public float minDamage = 0,
            maxDamage = 0,
            damageGain,
            heavyAttackDamageMultiplier = 2,
            pushDamageMultiplier = 0.1f,
            backStabDamageMultiplier = 1f,
            armorPenetration,
            damageIncreasePerCharge;

        [FoldoutGroup("Criticals", VisibleIf = nameof(IsWeapon)), Tooltip("Additional multiplier added to HeroStats multiplier")]
        public float criticalDamageMultiplier,
            weakSpotDamageMultiplier,
            sneakDamageMultiplier;
        
        [FoldoutGroup("DamageType", AnimateVisibility = false, VisibleIf = nameof(IsWeapon))] public DamageType damageType;
        [TableList, FoldoutGroup("DamageType"), InfoBox("subTypes must add up to 100%", InfoMessageType.Error, nameof(SubDmgTypesSumIncorrect))] 
        public List<DamageTypeDataConfig> damageSubTypes = new ();
        
        [CustomValueDrawer("@Awaken.TG.EditorOnly.OdinHelpers.Space(10)"), ShowInInspector] string _space_ODIN2;
        // --- Armor & Blocking
        [FoldoutGroup("Armor&Blocking"), ShowIf(nameof(IsArmorOrShield))]
        public float armor = 0,
            armorGain = 0;
        [VerticalGroup("Armor&Blocking/IsWeapon", VisibleIf = nameof(IsWeapon)), Range(0, 100), ShowIf(nameof(IsWeapon))] public int blockDamageReductionPercent = 50;
        [VerticalGroup("Armor&Blocking/IsWeapon")] public float blockAngle = 45;
        [VerticalGroup("Armor&Blocking/IsWeapon")] public float blockGain = 1;
        // --- Force
        [FoldoutGroup("Force", VisibleIf = nameof(IsWeapon))] public float forceDamage = 1;
        [FoldoutGroup("Force")] public float forceDamageGain;
        [FoldoutGroup("Force")] public float forceDamagePushMultiplier = 3;
        [FoldoutGroup("Force")] public float ragdollForce = 5;
        // --- Poise
        [FoldoutGroup("PoiseDamage", VisibleIf = nameof(IsWeapon))] public float poiseDamage = 1;
        [FoldoutGroup("PoiseDamage")] public float poiseDamageGain;
        [FoldoutGroup("PoiseDamage")] public float poiseDamageHeavyAttackMultiplier = 1.5f;
        [FoldoutGroup("PoiseDamage")] public float poiseDamagePushMultiplier = 3;
        
        [CustomValueDrawer("@Awaken.TG.EditorOnly.OdinHelpers.Space(10)"), ShowInInspector] string _space_ODIN3;
        // --- Misc Features
        [FoldoutGroup("Misc"), ShowIf(nameof(IsWeapon)), Range(0, 2)] public float npcDamageMultiplier = 1;
        [FoldoutGroup("Misc"), ShowIf(nameof(IsWeapon)), InlineButton(nameof(ResetAps)), Tooltip("Baked by tool based on weapon animations.")]
        public float attacksPerSecond = -1;
        [FoldoutGroup("Misc"), ShowIf(nameof(IsWeapon))]
        public float randomnessModifier = 1;
        [Title("Ranged")]
        [FoldoutGroup("Misc"), ShowIf(nameof(IsRanged))] public float rangedZoomModifier = -1;
        [FoldoutGroup("Misc"), ShowIf(nameof(IsRanged))] public float bowDrawSpeedModifier = 1;
        // --- Magic
        [FoldoutGroup("Magic"), ShowIf(nameof(IsMagicOrWeapon))] public float lightCastManaCost = 0f;
        [FoldoutGroup("Magic"), ShowIf(nameof(IsMagicOrWeapon))] public float heavyCastManaCost = 0f;
        [FoldoutGroup("Magic"), ShowIf(nameof(IsMagicOrWeapon))] public float heavyCastManaCostPerSecond = 0f;
        [FoldoutGroup("Magic"), ShowIf(nameof(IsMagicOrWeapon)), Range(0f, 1f)] public float magicHeldSpeedMultiplier = 0.3f;
        
        public Element SpawnElement() {
            return new ItemStats();
        }

        public bool IsMine(Element element) => element is ItemStats;
        
        public DamageTypeData GetDamageTypeData() {
            var subTypes = new DamageTypeDataPart[damageSubTypes.Count];
            for (int i = 0; i < damageSubTypes.Count; i++) {
                subTypes[i] = DamageTypeDataConfig.Construct(damageSubTypes[i], damageType);
            }
            return new DamageTypeData(damageType, subTypes.ToList());
        }
        
        // === Editor
        void ResetAps() => attacksPerSecond = -1f;

        [ShowInInspector, ShowIf(nameof(IsWeapon))]
        public float Dps => attacksPerSecond * (minDamage + maxDamage) * 0.5f;

        ItemTemplate _template;
        ItemTemplate Template => _template ??= GetComponent<ItemTemplate>();
        
        public bool IsWeapon => Template.IsWeapon || Template.IsArrow || Template.IsThrowable;
        public bool IsArmor => Template.IsArmor;
        public bool IsRanged => Template.IsRanged;
        public bool IsShield => Template.IsShield;
        public bool IsMelee => Template.IsMelee;
        public bool IsMagic => Template.IsMagic;
        public float Weight => Template.Weight;

        public bool IsRangedOrShield => IsRanged || IsShield;
        public bool IsArmorOrShield => IsArmor || IsShield;
        public bool IsMagicOrWeapon => IsMagic || IsWeapon;
        
        bool SubDmgTypesSumIncorrect() {
            return damageSubTypes?.Sum(s => s.percentage) != 100;
        }

        [Button]
        public bool SetDefaultPoiseDamage() {
            if (Template.IsAbstract || Template.TemplateType == TemplateType.ForRemoval) {
                return false;
            }
            
            float averageDamage = (minDamage + maxDamage) / 2f;
            if (Template.IsDagger) {
                poiseDamage = minDamage;
            } else if (Template.IsRanged) {
                poiseDamage = minDamage;
            } else if (Template.IsMagic) {
                poiseDamage = minDamage;
            } else if (Template.IsOneHanded) {
                if (Template.name.Contains("sword", StringComparison.InvariantCultureIgnoreCase)) {
                    poiseDamage = averageDamage;
                } else if (Template.name.Contains("axe", StringComparison.InvariantCultureIgnoreCase)) {
                    poiseDamage = (averageDamage + maxDamage) / 2f;
                } else if (Template.name.Contains("hammer", StringComparison.InvariantCultureIgnoreCase) 
                           || Template.name.Contains("club", StringComparison.InvariantCultureIgnoreCase)
                           || Template.name.Contains("mace", StringComparison.InvariantCultureIgnoreCase)
                           || Template.name.Contains("staff", StringComparison.InvariantCultureIgnoreCase)
                           || Template.name.Contains("shield", StringComparison.InvariantCultureIgnoreCase)) {
                    poiseDamage = maxDamage;
                } else {
                    Debug.LogError("OneHanded Template that is not sword/axe/hammer, falling back to avg dmg. " + Template.name, Template.gameObject);
                    poiseDamage = averageDamage;
                }
            } else if (Template.IsTwoHanded) {
                if (Template.name.Contains("sword", StringComparison.InvariantCultureIgnoreCase)) {
                    poiseDamage = averageDamage;
                } else if (Template.name.Contains("axe", StringComparison.InvariantCultureIgnoreCase)) {
                    poiseDamage = (averageDamage + maxDamage) / 2f;
                } else if (Template.name.Contains("hammer", StringComparison.InvariantCultureIgnoreCase) 
                           || Template.name.Contains("club", StringComparison.InvariantCultureIgnoreCase)
                           || Template.name.Contains("mace", StringComparison.InvariantCultureIgnoreCase)
                           || Template.name.Contains("staff", StringComparison.InvariantCultureIgnoreCase)) {
                    poiseDamage = maxDamage;
                } else if (Template.name.Contains("polearm", StringComparison.InvariantCultureIgnoreCase)) {
                    poiseDamage = minDamage;
                } else {
                    Debug.LogError("TwoHanded Template that is not sword/axe/hammer/polearm, falling back to avg dmg. " + Template.name, Template.gameObject);
                    poiseDamage = averageDamage;
                }
            }

            return true;
        }

        [Button]
        public void SetDefaultForceDamage() {
            forceDamage = poiseDamage * 0.5f;
        }
    }
}