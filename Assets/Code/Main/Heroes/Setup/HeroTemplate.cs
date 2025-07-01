using System;
using System.Collections.Generic;
using System.Linq;
using Awaken.TG.Main.Character;
using Awaken.TG.Main.Fights.DamageInfo;
using Awaken.TG.Main.Fights.NPCs;
using Awaken.TG.Main.Heroes.Items.LootTables;
using Awaken.TG.Main.Skills;
using Awaken.TG.Main.Templates;
using Awaken.TG.Utility.Attributes.List;
using Awaken.Utility.Collections;
using Awaken.Utility.GameObjects;
using Awaken.Utility.Times;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Awaken.TG.Main.Heroes.Setup {
    /// <summary>
    /// Template for individual heroes.
    /// </summary>
    [Searchable]
    public class HeroTemplate : Template, CharacterStats.ITemplate, AliveStats.ITemplate, StatusStats.ITemplate {
        // === Editable information

        [Title("Combat stats")]
        public int startLevel = 1;
        public int baseStrength;
        public float baseStrengthLinear = 5f;
        public int baseEvasion;
        public int baseArmorMultiplier = 1;
        public int baseArmor;
        public int baseResistance;
        [ListDrawerSettings(CustomAddFunction = nameof(AddDefaultDamageReceivedMultiplier))]
        public List<DamageReceivedMultiplierDataConfig> damageReceivedMultipliers = new ();
        public int baseStatusResistance;
        public int startTalentPoints;
        public int startBaseStatPoints;
        [UnityEngine.Scripting.Preserve] public float trapDamageMultiplier = 0.5f;
        
        [Title("Basic stats")]
        public int maxHealth;
        public int maxStamina = 100;
        public float staminaUsageMultiplier = 1f;
        public int maxMana = 100;
        public float manaUsageMultiplier = 1f;
        public float baseManaRegen = 0.5f;
        public float baseManaRegenPercentage = 0f;
        [UnityEngine.Scripting.Preserve] public float encumbrancePenalty = 0.5f;
        public float encumbranceLimit = 200;
        
        [Title("Damage")] 
        public float criticalChance = .05F;
        public float criticalDamageMultiplier = 0.5F;
        public float sneakDamageMultiplier = 0.5F;
        public float backStabDamageMultiplier = 2F;
        public float meleeSneakDamageMultiplier = 0F;
        public float weakSpotDamageMultiplier = 0.5F;
        public float meleeWeakSpotDamageMultiplier = 0F;
        public float meleeCriticalChance = 0F;
        public float rangedCriticalChance = 0F;
        public float magicCriticalChance = 0F;
        public float itemStaminaCostMultiplier = 1F;
        public float dualWieldHeavyAttackCostMultiplier = 2f;
        public int summonLimit = 5;

        [Title("Build Up stats")]
        [SerializeField]
        StatusStatsValues statusStats = new();
        
        public float wyrdSkillDuration = 15f;

        [Title("Hero Controller Data")] 
        public HeroControllerData heroControllerData;
        public CharacterGroundedData heroGroundedData;
        
        // -- skills
        [Title("Skills")]
        
        [SerializeField] SkillReference[] initialSkills = Array.Empty<SkillReference>();

        [Title("Items")]

        [List(ListEditOption.Buttons | ListEditOption.ListLabel)]
        public ItemSpawningData[] initialItems = Array.Empty<ItemSpawningData>();
        [InfoBox("Early access owners bonus items")]
        public ItemSpawningData[] bonusItems = 
            #if UNITY_EDITOR
                new ItemSpawningData[0];
            #else
                Array.Empty<ItemSpawningData>();
            #endif
        
        public DateTime bonusItemsDate => bonusDateUTC.AsDateTime();
        [SerializeField, InlineProperty]
        SerializableDate bonusDateUTC;

        [Title("Economy")]
        public float buyModifier = 1.0f;
        public float sellModifier = 0.1f;
        
        [Title("Campfire tutorial settings")] 
        [SerializeField] int campfireTutorialAddLevels = 0;
        [SerializeField] int campfireTutorialAddTalentPoints = 1;
        [SerializeField] int campfireTutorialAddBaseStats = 5;
        
        public PooledList<NpcTemplate> AbstractTypes => this.Abstracts<NpcTemplate>();
        
        public ref StatusStatsValues StatusStats => ref statusStats;

        // === Creation
        public static HeroTemplate CreateNewHero(string name) {
            HeroTemplate template = GameObjects.WithSingleBehavior<HeroTemplate>(name: name);
            return template;
        }
        
        // == Getters
        public IEnumerable<Skill> InitialSkills => initialSkills.Select(reference => reference.CreateSkill());

        public int Level => startLevel;
        public int TalentPoints => startTalentPoints;
        public int BaseStatPoints => startBaseStatPoints;
        public int MaxStamina => maxStamina;
        public float StaminaRegen => heroControllerData.staminaRegenPerTick;
        public float StaminaUsageMultiplier => staminaUsageMultiplier;
        public int MaxMana => maxMana;
        public float ManaUsageMultiplier => manaUsageMultiplier;
        public float ManaRegen => baseManaRegen;
        public float ManaRegenPercentage => baseManaRegenPercentage;
        public float Strength => baseStrength;
        public float StrengthLinear => baseStrengthLinear;
        public float Evasion => baseEvasion;
        public float Resistance => baseResistance;
        public int MaxHealth => maxHealth;
        public float ArmorMultiplier => baseArmorMultiplier;
        public int Armor => baseArmor;
        public float StatusResistance => baseStatusResistance;
        public float ForceStumbleThreshold => 0f;
        public float TrapDamageMultiplier => 0.5f;
        public int CampfireTutorialAddLevels => campfireTutorialAddLevels;
        public int CampfireTutorialAddTalentPoints => campfireTutorialAddTalentPoints;
        public int CampfireTutorialAddBaseStats => campfireTutorialAddBaseStats;
        
        public DamageReceivedMultiplierData GetDamageReceivedMultiplierData() {
            var parts = new DamageReceivedMultiplierDataPart[damageReceivedMultipliers.Count];
            for (int i = 0; i < damageReceivedMultipliers.Count; i++) {
                parts[i] = DamageReceivedMultiplierDataConfig.Construct(damageReceivedMultipliers[i]);
            }
            return new DamageReceivedMultiplierData(parts);
        }

        DamageReceivedMultiplierDataConfig AddDefaultDamageReceivedMultiplier() => DamageReceivedMultiplierDataConfig.Default;
    }
}
