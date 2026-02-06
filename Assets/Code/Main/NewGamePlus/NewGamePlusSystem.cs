using System.Collections.Generic;
using System.Linq;
using Awaken.TG.Main.Character;
using Awaken.TG.Main.Fights.NPCs;
using Awaken.TG.Main.Heroes;
using Awaken.TG.Main.Heroes.Items.Attachments;
using Awaken.TG.Main.Heroes.Setup;
using Awaken.TG.Main.Heroes.Stats;
using Awaken.TG.Main.Heroes.Stats.Tweaks;
using Awaken.TG.MVC;
using Awaken.TG.MVC.Domains;
using Awaken.TG.MVC.Events;
using Awaken.TG.Utility.Attributes;
using Awaken.Utility;
using Awaken.Utility.Extensions;
using Unity.Mathematics;

namespace Awaken.TG.Main.NewGamePlus {
    public partial class NewGamePlusSystem : Model {
        // Armor reduction for the hero, so Armor values can scale. 
        const float BonusHeroArmor = -50.0f; 
        // X*sqrRoot(ngLevel), Multiplier for the hero.
        const float HeroExpMultiplier = 3.0f; 
        // X*ngLevel, Multiplier for the hero.
        const float HeroWealthMultiplier = 1.3f;
        // X*nglevel, Bonus item level per New Game Plus level.
        const int BonusItemLevel = 10;
        // X*sqrRoot(nglevel), Bonus quest XP level per sqr root of New Game Plus level , used for calculating XP.
        const int BonusQuestXPLevel = 30;
        const int BonusQuestXPValueIfCustom = 500;
        const int BonusEnemyXPLevel = 20;
        const int BonusEnemyXPValueIfCustom = 350;
        // X*ngLevel, Bonus for item stat requirements per New Game Plus level, added after mapping
        const int BonusItemStatRequirement = 0; 
        // X^ngLevel, Multiplier for item upgrades cost per New Game Plus level.
        const float ItemUpgradesServiceCostMultiplier = 1.0f;
        const float ItemUpgradesCobwebCostMultiplier = 1.0f; 

        static readonly (int, float)[] EnemyHealthMap = new (int, float)[] {
            (0, 0f), // Critter,
            (250, 1f), // Trash,
            (750, 1.2f), // Normal
            (3000, 1f), // Elite
            (3500, 1f), // MiniBoss
            (4000, 1f), // Boss
            (500, 1f), // HeroSummon
        };
        
        static readonly (int, float)[] EnemyStaminaMap = new (int, float)[] {
            (0, 0f), // Critter,
            (50, 1f), // Trash,
            (50, 1.2f), // Normal
            (75, 1f), // Elite
            (100, 1f), // MiniBoss
            (100, 1f), // Boss
            (50, 1f), // HeroSummon
        };
        
        static readonly (int, float)[] EnemyDamageMap = new (int, float)[] {
            (0, 0f), // Critter,
            (30, 0f), // Trash,
            (40, 0.2f), // Normal
            (80, 0.2f), // Elite
            (80, 0.2f), // MiniBoss
            (80, 0.2f), // Boss
            (30, 0.2f), // HeroSummon
        };

        static readonly int[] EnemyTierMap1 = new[] {
            5, // 0 -> 5
            5, // 1 -> 5
            5, // 2 -> 5
            6, // 3 -> 6
            6, // 4 -> 6
            7, // 5 -> 7
            7, // 6 -> 7
        };
        static readonly int[] EnemyTierMap2 = new[] {
            5, // 0 -> 5
            5, // 1 -> 5
            5, // 2 -> 5
            6, // 3 -> 6
            6, // 4 -> 6
            7, // 5 -> 7
            7, // 6 -> 7
        };
        static readonly int[] EnemyTierMapOther = new[] {
            5, // 0 -> 5
            5, // 1 -> 5
            5, // 2 -> 5
            6, // 3 -> 6
            6, // 4 -> 6
            7, // 5 -> 7
            7, // 6 -> 7
        };
        
        static readonly int[] StatRequirementMap = new[] {
            0, // 0 -> 0
            1, // 1 -> 1
            2, // 2 -> 2
            3, // 3 -> 3
            4, // 4 -> 4
            5, // 5 -> 5
            6, // 6 -> 6
            7, // 7 -> 7
            8, // 8 -> 8
            9, // 9 -> 9
            10, // 10 -> 10
            11, // 11 -> 11
            12, // 12 -> 12
            13, // 13 -> 13
            14, // 14 -> 14
            15, // 15 -> 15
            16, // 16 -> 16
            17, // 17 -> 17
            18, // 18 -> 18
            19, // 19 -> 19
            20, // 20 -> 20
        };
        
        public static int Level { get; private set; } = 0;
        [UnityEngine.Scripting.Preserve] public static float ArmorLose { get; private set; } = 0;
        public static int CalculatedBonusItemLevel { get; private set; } = 0;
        public static int CalculatedBonusQuestXPLevel { get; private set; } = 0;
        public static int CalculatedBonusQuestXPValueIfCustom { get; private set; } = 0;
        public static int CalculatedBonusEnemyXPLevel { get; private set; } = 0;
        public static int CalculatedBonusEnemyXPValueIfCustom { get; private set; } = 0;
        public static float CalculatedXPMultiplier { get; private set; } = 1;

        public override ushort TypeForSerialization => SavedModels.NewGamePlusSystem;

        public override Domain DefaultDomain => Domain.Gameplay;

        [Saved] int _level = 0;
        StatTweak _armorBonusTweak, _expMultiplier, _wealthMultiplier;

        [UnityEngine.Scripting.Preserve] IEnumerable<StatTweak> Tweaks => new[] { _armorBonusTweak, _expMultiplier, _wealthMultiplier }.AsEnumerable();

        public new static class Events {
            public static readonly Event<NewGamePlusSystem, int> NewGamePlusStarted = new(nameof(NewGamePlusStarted));
        }

        public NewGamePlusSystem(int newGamePlusLevel) {
            if (newGamePlusLevel <= 0) {
                _level = 0;
                Reset();
            } else {
                _level = newGamePlusLevel;
                CalculateValues(_level);
            }
        }
        
        protected override void OnInitialize() {
            this.Trigger(Events.NewGamePlusStarted, _level);
        }

        protected override void OnRestore() {
            CalculateValues(_level);
        }

        protected override void OnFullyInitialized() {
            if (Level <= 0) {
                return;
            }
            if (Hero.Current is { IsFullyInitialized: true }) {
                CreateHeroTweaks();
            } else {
                World.EventSystem.ListenTo(EventSelector.AnySource, World.Events.ModelFullyInitialized<Hero>(), this, CreateHeroTweaks);
            }
        }
        
        public static int GetEnemyTier(int enemyTier) {
            if (Level <= 0) {
                return enemyTier;
            }

            int[] map = GetEnemyTierMap();
            if (enemyTier > map.Length - 1) {
                return enemyTier;
            }
            return map[enemyTier];
        }

        public static void GetAllStatsRequirements(int ngLevel, ItemStatsRequirementsAttachment dataSource, out int str, out int dex, out int spi, out int per, out int end,  out int pra) {
            str = GetItemStatsRequirementValue(ngLevel, dataSource.strengthRequired);
            dex = GetItemStatsRequirementValue(ngLevel, dataSource.dexterityRequired);
            spi = GetItemStatsRequirementValue(ngLevel, dataSource.spiritualityRequired);
            per = GetItemStatsRequirementValue(ngLevel, dataSource.perceptionRequired);
            end = GetItemStatsRequirementValue(ngLevel, dataSource.enduranceRequired);
            pra = GetItemStatsRequirementValue(ngLevel, dataSource.practicalityRequired);
        }
        
        static int GetItemStatsRequirementValue(int ngLevel, int requiredStatValue) {
            if (ngLevel <= 0 || requiredStatValue <= 0) {
                return requiredStatValue;
            }

            int maxValue = StatRequirementMap.Length - 1;
            if (requiredStatValue > maxValue) {
                return StatRequirementMap[maxValue] + requiredStatValue - maxValue + BonusItemStatRequirement * (ngLevel - 1);
            }
            return StatRequirementMap[requiredStatValue] + BonusItemStatRequirement * (ngLevel - 1);
        }
        
        public static float GetItemUpgradeServiceCostMultiplier(int ngLevel) {
            return math.pow(ItemUpgradesServiceCostMultiplier, ngLevel);
        }
        
        public static float GetItemUpgradeCobwebCostMultiplier(int ngLevel) {
            return math.pow(ItemUpgradesCobwebCostMultiplier, ngLevel);
        }
        
        public static void GetAllAliveStats(AliveStats.ITemplate template, int ngLevel, int heroLevel, out int health) {
            GetBaseValues(template, heroLevel, out health);
            if (ngLevel <= 0 || template is not NpcTemplate npcTemplate) {
                return;
            }
            (int flat, float multiplier) bonus = EnemyHealthMap[npcTemplate.NpcType.ToInt()];
            health = (int) GetMapMultipliedStatValue(bonus, ngLevel, health);
            
            void GetBaseValues(AliveStats.ITemplate t, int lvl, out int baseHealth) {
                if (t is ScalingNpcTemplate scalingNpcTemplate) {
                    baseHealth = scalingNpcTemplate.ScaledMaxHealth(lvl);
                } else {
                    baseHealth = t.MaxHealth;
                }
            }
        }
        
        public static void GetAllCharacterStats(CharacterStats.ITemplate template, int ngLevel, int heroLevel, out int stamina) {
            GetBaseValues(template, heroLevel, out stamina);
            if (ngLevel <= 0 || template is not NpcTemplate npcTemplate) {
                return;
            }
            (int flat, float multiplier) bonus = EnemyStaminaMap[npcTemplate.NpcType.ToInt()];
            stamina = (int) GetMapMultipliedStatValue(bonus, ngLevel, stamina);

            void GetBaseValues(CharacterStats.ITemplate t, int lvl, out int baseStamina) {
                if (t is ScalingNpcTemplate scalingNpcTemplate) {
                    baseStamina = scalingNpcTemplate.ScaledMaxStamina(lvl);
                } else {
                    baseStamina = t.MaxStamina;
                }
            }
        }
        
        public static void GetAllNpcStats(NpcElement npc, NpcTemplate template, int ngLevel, int heroLevel, out float meleeDamage, out float rangedDamage, out float magicDamage, out float poiseThreshold, out float forceStumbleThreshold) {
            GetBaseValues(template, heroLevel, out meleeDamage, out rangedDamage, out magicDamage, out poiseThreshold, out forceStumbleThreshold, out var maxHealth);
            
            if (ngLevel <= 0) {
                return;
            }
            
            (int flat, float multiplier) bonus = EnemyDamageMap[template.NpcType.ToInt()];
            meleeDamage = GetMapMultipliedStatValue(bonus, ngLevel, meleeDamage);
            rangedDamage = GetMapMultipliedStatValue(bonus, ngLevel, rangedDamage);
            magicDamage = GetMapMultipliedStatValue(bonus, ngLevel, magicDamage);
            var currentMaxHealth = npc.MaxHealth.ValueForSave;
            poiseThreshold = (poiseThreshold / maxHealth) * currentMaxHealth;
            forceStumbleThreshold = (forceStumbleThreshold / maxHealth) * currentMaxHealth;

            void GetBaseValues(NpcTemplate t, int lvl, out float baseMeleeDamage, out float baseRangedDamage, out float baseMagicDamage, out float basePoiseThreshold, out float baseForceStumbleThreshold, out float baseMaxHealth) {
                if (t is ScalingNpcTemplate scalingNpcTemplate) {
                    baseMeleeDamage = scalingNpcTemplate.ScaledMeleeDamage(lvl);
                    baseRangedDamage = scalingNpcTemplate.ScaledRangedDamage(lvl);
                    baseMagicDamage = scalingNpcTemplate.ScaledMagicDamage(lvl);
                    basePoiseThreshold = scalingNpcTemplate.ScaledPoiseThreshold(lvl);
                    baseForceStumbleThreshold = scalingNpcTemplate.ScaledForceStumbleThreshold(lvl);
                    baseMaxHealth = scalingNpcTemplate.ScaledMaxHealth(lvl);
                } else {
                    baseMeleeDamage = t.MeleeDamage;
                    baseRangedDamage = t.RangedDamage;
                    baseMagicDamage = t.MagicDamage;
                    basePoiseThreshold = t.PoiseThreshold;
                    baseForceStumbleThreshold = t.ForceStumbleThreshold;
                    baseMaxHealth = t.MaxHealth;
                }
            }
        }

        static float GetMapMultipliedStatValue((int flat, float multiplier) bonus, int ngLevel, float baseDamage) {
            return (baseDamage + bonus.flat) * (1f + bonus.multiplier * ngLevel);
        }

        public static void Reset() {
            Level = 0;
            ArmorLose = 0;

            CalculatedBonusItemLevel = 0;
            CalculatedBonusQuestXPLevel = 0;
            CalculatedBonusQuestXPValueIfCustom = 0;
            CalculatedBonusEnemyXPLevel = 0;
            CalculatedBonusEnemyXPValueIfCustom = 0;
            CalculatedXPMultiplier = 1f;
        }

        static void CalculateValues(int level) {
            Level = level;
            ArmorLose = CalculateHeroArmorReduction(level);
            
            CalculatedBonusItemLevel = CalculateBonusItemLevelValue(level);
            CalculatedBonusQuestXPLevel = CalculateBonusQuestXPLevelValue(level);
            CalculatedBonusQuestXPValueIfCustom = CalculateBonusQuestXPValueIfCustomValue(level);
            CalculatedBonusEnemyXPLevel = CalculateBonusEnemyXPLevelValue(level);
            CalculatedBonusEnemyXPValueIfCustom = CalculateBonusEnemyXPValueIfCustomValue(level);
        }

        void CreateHeroTweaks() {
            float armorReduction = CalculateHeroArmorReduction(Level);
            _armorBonusTweak = new(Hero.Current.AliveStats.Armor, armorReduction, TweakPriority.Override, OperationType.AddPreMultiply, this);
            CalculatedXPMultiplier = CalculateExpMultiplier(Level);
            _expMultiplier = new(Hero.Current.HeroMultStats.ExpMultiplier, CalculatedXPMultiplier, TweakPriority.Override, OperationType.Multi, this);
            float wealthMultiplier = CalculateWealthMultiplier(Level);
            _wealthMultiplier  = new(Hero.Current.HeroMultStats.WealthMultiplier, wealthMultiplier, TweakPriority.Override, OperationType.Multi, this);
            
            _armorBonusTweak.MarkedNotSaved = true;
            _expMultiplier.MarkedNotSaved = true;
            _wealthMultiplier.MarkedNotSaved = true;
        }
        
        static float CalculateHeroArmorReduction(int ngLevel) {
            return ngLevel * BonusHeroArmor;
        }
        
        static float CalculateExpMultiplier(int ngLevel) {
            if (ngLevel == 0) {
                return 1f;
            }
            return math.sqrt(ngLevel) * HeroExpMultiplier;
        }
        
        static float CalculateWealthMultiplier(int ngLevel) {
            if (ngLevel == 0) {
                return 1f;
            }
            return ngLevel * HeroWealthMultiplier;
        }
        
        public static int CalculateBonusItemLevelValue(int ngLevel) {
            return ngLevel * BonusItemLevel;
        }

        static int CalculateBonusQuestXPLevelValue(int ngLevel) {
            if (ngLevel == 0) {
                return 0;
            }
            return (int) (math.sqrt(ngLevel) * BonusQuestXPLevel);
        }
        
        static int CalculateBonusQuestXPValueIfCustomValue(int ngLevel) {
            if (ngLevel == 0) {
                return 0;
            }
            return (int) (math.sqrt(ngLevel) * BonusQuestXPValueIfCustom);
        }
        
        static int CalculateBonusEnemyXPLevelValue(int ngLevel) {
            if (ngLevel == 0) {
                return 0;
            }
            return (int) (math.sqrt(ngLevel) * BonusEnemyXPLevel);
        }
        
        static int CalculateBonusEnemyXPValueIfCustomValue(int ngLevel) {
            if (ngLevel == 0) {
                return 0;
            }
            return (int) (math.sqrt(ngLevel) * BonusEnemyXPValueIfCustom);
        }
        
        static int[] GetEnemyTierMap() {
            return Level switch {
                1 => EnemyTierMap1,
                2 => EnemyTierMap2,
                _ => EnemyTierMapOther
            };
        }

        protected override void OnDiscard(bool fromDomainDrop) {
            Reset();
        }
    }
}
