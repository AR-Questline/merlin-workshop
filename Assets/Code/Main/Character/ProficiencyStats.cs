using System.Collections.Generic;
using Awaken.TG.Main.General.Configs;
using Awaken.TG.Main.General.StatTypes;
using Awaken.TG.Main.Heroes;
using Awaken.TG.Main.Heroes.Stats;
using Awaken.TG.Main.UI.HUD.AdvancedNotifications;
using Awaken.TG.Main.UI.HUD.AdvancedNotifications.LeftScreen.Proficiency;
using Awaken.TG.MVC;
using Awaken.TG.MVC.Elements;
using Awaken.TG.Utility;
using Awaken.TG.Utility.Attributes;
using Awaken.Utility;
using Awaken.Utility.Debugging;
using UnityEngine;

namespace Awaken.TG.Main.Character {
    public partial class ProficiencyStats : Element<Hero> {
        public override ushort TypeForSerialization => SavedModels.ProficiencyStats;

        [Saved] ProficiencyStatsWrapper _wrapper;
        
        //Strength base proficiencies
        public LimitedStat OneHanded { get; private set; }
        public LimitedStat TwoHanded { get; private set; }
        public LimitedStat Unarmed { get; private set; }
        public LimitedStat Shield { get; private set; }
        public LimitedStat Athletics { get; private set; }
        
        //Armors
        public LimitedStat LightArmor { get; private set; }
        public LimitedStat MediumArmor { get; private set; }
        public LimitedStat HeavyArmor { get; private set; }
        
        //Dexterity base proficiencies
        public LimitedStat Archery { get; private set; }
        public LimitedStat Evasion { get; private set; }
        public LimitedStat Acrobatics { get; private set; }
        public LimitedStat Sneak { get; private set; }
        public LimitedStat Theft { get; private set; }
        
        //Intelligence base proficiencies
        public LimitedStat Magic { get; private set; }

        public LimitedStat Alchemy { get; private set; }
        public LimitedStat Cooking { get; private set; }
        public LimitedStat Handcrafting { get; private set; }
        //Animals
        public LimitedStat AnimalWeapon { get; private set; }

        [Saved] Dictionary<StatType, float> _statXPDictionary = new();
        
        public const int ProficiencyBaseValue = 10;
        const int MinProficiencyLevel = 0;
        const int MaxProficiencyLevel = 100;
        
        static bool DebugMode => SafeEditorPrefs.GetBool("debug.proficiency");
        
        protected override void OnInitialize() {
            _wrapper.Initialize(this);
        }
        
        public static void CreateFromFightStats(Hero hero) {
            ProficiencyStats stats = new();
            hero.AddElement(stats);
        }


        public void TryAddXP(ProfStatType targetStatToRaiseXPOf, float amountOfXPToAdd) {
            if(ParentModel.Stat(targetStatToRaiseXPOf).ModifiedInt >= MaxProficiencyLevel) {
                return;
            }
            
            amountOfXPToAdd *= Hero.Current.HeroMultStats.ProfMultiplier;
            
            Stat retrievedFromParent = targetStatToRaiseXPOf.RetrieveFrom(ParentModel);
            _statXPDictionary.TryAdd(retrievedFromParent.Type, 0);
            
            float tempXPQuantity = _statXPDictionary[retrievedFromParent.Type] + amountOfXPToAdd;

            float xpNecessaryForNextLevel = GetXPNeededForNextLevel(retrievedFromParent.ModifiedInt);

            // Leveled
            while (tempXPQuantity > xpNecessaryForNextLevel) {
                if (DebugMode) {
                    Log.Minor?.Info("Leveled stat: " + retrievedFromParent.Type.EnumName +  
                                                                                " >> level " + retrievedFromParent.ModifiedInt + 
                                                                                " -> " + (retrievedFromParent.ModifiedInt + 1) + 
                                                                                "\n XP Before: " + _statXPDictionary[retrievedFromParent.Type] + 
                                                                                "\n XP for this level: " + xpNecessaryForNextLevel + 
                                                                                "\n XP after leveling deduction: " + (tempXPQuantity - xpNecessaryForNextLevel));
                }

                tempXPQuantity -= xpNecessaryForNextLevel;
                retrievedFromParent.IncreaseBy(1);
                
                // Reward exp to Hero
                float multi = World.Services.Get<GameConstants>().ProficiencyLvlHeroExpMulti;
                ParentModel.Development.RewardExpAsPercentOfNextLevel(multi);

                var proficiencyData = new ProficiencyData(targetStatToRaiseXPOf, retrievedFromParent.ModifiedInt);
                AdvancedNotificationBuffer.Push<ProficiencyNotificationBuffer>(new ProficiencyNotification(proficiencyData));
            }
            
            _statXPDictionary[retrievedFromParent.Type] = tempXPQuantity;
        }

        public float GetProgressToNextLevel(ProfStatType stat) {
            if (_statXPDictionary.TryGetValue(stat, out var value)) {
                Stat retrievedFromParent = stat.RetrieveFrom(ParentModel);
                if (retrievedFromParent.ModifiedInt >= MaxProficiencyLevel) {
                    return 1;
                }
                
                float xpNecessaryForNextLevel = GetXPNeededForNextLevel(retrievedFromParent.ModifiedInt);
                return value / xpNecessaryForNextLevel;
            } 
            return 0f;
        }

        /// <summary>
        /// XP Cost(skillLevel) = Skill Improve Mult * (skillLevel+15)^1.95 + Skill Improve Offset
        /// > Skill Improve Mult = Zwiększa ogólną trudność wbicia poziomu skilla
        /// > Skill Improve Offset = Zwiększa trudność wbicia pierwszego poziomu skilla
        /// </summary>
        /// <param name="currentLevel"></param>
        /// <returns>The xp needed for the next level</returns>
        float GetXPNeededForNextLevel(float currentLevel) {
            float skillImproveMult = World.Services.Get<GameConstants>().skillImproveMult, 
                skillImproveOffset = World.Services.Get<GameConstants>().skillImproveMOffset;
            
            return skillImproveMult * Mathf.Pow(currentLevel + 4, 1.5f) + skillImproveOffset;
        }

        // === Persistence

        void OnBeforeWorldSerialize() {
            _wrapper.PrepareForSave(this);
        }
        
        public partial struct ProficiencyStatsWrapper {
            public ushort TypeForSerialization => SavedTypes.ProficiencyStatsWrapper;

            [Saved(0f)] float OneHandedDif;
            [Saved(0f)] float TwoHandedDif;
            [Saved(0f)] float UnarmedDif;
            [Saved(0f)] float ShieldDif;
            [Saved(0f)] float AthleticsDif;
            [Saved(0f)] float LightArmorDif;
            [Saved(0f)] float MediumArmorDif;
            [Saved(0f)] float HeavyArmorDif;
            [Saved(0f)] float ArcheryDif;
            [Saved(0f)] float EvasionDif;
            [Saved(0f)] float AcrobaticsDif;
            [Saved(0f)] float SneakDif;
            [Saved(0f)] float TheftDif;
            [Saved(0f)] float MagicDif;
            [Saved(0f)] float AlchemyDif;
            [Saved(0f)] float CookingDif;
            [Saved(0f)] float HandcraftingDif;
            [Saved(0f)] float AnimalWeaponDif;
            [Saved(0f)] float LyingDif;
            [Saved(0f)] float PersuasionDif;
            [Saved(0f)] float IntimidationDif;

            public void Initialize(ProficiencyStats profStats) {
                Hero hero = profStats.ParentModel;
                
                profStats.OneHanded = new LimitedStat(hero, ProfStatType.OneHanded, ProficiencyBaseValue + OneHandedDif, MinProficiencyLevel, MaxProficiencyLevel);
                profStats.TwoHanded = new LimitedStat(hero, ProfStatType.TwoHanded, ProficiencyBaseValue + TwoHandedDif, MinProficiencyLevel, MaxProficiencyLevel);
                profStats.Unarmed = new LimitedStat(hero, ProfStatType.Unarmed, ProficiencyBaseValue + UnarmedDif, MinProficiencyLevel, MaxProficiencyLevel);
                profStats.Shield = new LimitedStat(hero, ProfStatType.Shield, ProficiencyBaseValue + ShieldDif, MinProficiencyLevel, MaxProficiencyLevel);
                profStats.Athletics = new LimitedStat(hero, ProfStatType.Athletics, ProficiencyBaseValue + AthleticsDif, MinProficiencyLevel, MaxProficiencyLevel);
                
                profStats.LightArmor = new LimitedStat(hero, ProfStatType.LightArmor, ProficiencyBaseValue + LightArmorDif, MinProficiencyLevel, MaxProficiencyLevel);
                profStats.MediumArmor = new LimitedStat(hero, ProfStatType.MediumArmor, ProficiencyBaseValue + MediumArmorDif, MinProficiencyLevel, MaxProficiencyLevel);
                profStats.HeavyArmor = new LimitedStat(hero, ProfStatType.HeavyArmor, ProficiencyBaseValue + HeavyArmorDif, MinProficiencyLevel, MaxProficiencyLevel);
                
                profStats.Archery = new LimitedStat(hero, ProfStatType.Archery, ProficiencyBaseValue + ArcheryDif, MinProficiencyLevel, MaxProficiencyLevel);
                profStats.Evasion = new LimitedStat(hero, ProfStatType.Evasion, ProficiencyBaseValue + EvasionDif, MinProficiencyLevel, MaxProficiencyLevel);
                profStats.Acrobatics = new LimitedStat(hero, ProfStatType.Acrobatics, ProficiencyBaseValue + AcrobaticsDif, MinProficiencyLevel, MaxProficiencyLevel);
                profStats.Sneak = new LimitedStat(hero, ProfStatType.Sneak, ProficiencyBaseValue + SneakDif, MinProficiencyLevel, MaxProficiencyLevel);
                profStats.Theft = new LimitedStat(hero, ProfStatType.Theft, ProficiencyBaseValue + TheftDif, MinProficiencyLevel, MaxProficiencyLevel);
                
                profStats.Magic = new LimitedStat(hero, ProfStatType.Magic, ProficiencyBaseValue + MagicDif, MinProficiencyLevel, MaxProficiencyLevel);
                
                profStats.Alchemy = new LimitedStat(hero, ProfStatType.Alchemy, ProficiencyBaseValue + AlchemyDif, MinProficiencyLevel, MaxProficiencyLevel);
                profStats.Cooking = new LimitedStat(hero, ProfStatType.Cooking, ProficiencyBaseValue + CookingDif, MinProficiencyLevel, MaxProficiencyLevel);
                profStats.Handcrafting = new LimitedStat(hero, ProfStatType.Handcrafting, ProficiencyBaseValue + HandcraftingDif, MinProficiencyLevel, MaxProficiencyLevel);

                profStats.AnimalWeapon = new LimitedStat(hero, ProfStatType.AnimalWeapon, 1 + AnimalWeaponDif, 1, 1);
            }

            public void PrepareForSave(ProficiencyStats profStats) {
                OneHandedDif = profStats.OneHanded.BaseValue - ProficiencyBaseValue;
                TwoHandedDif = profStats.TwoHanded.BaseValue - ProficiencyBaseValue;
                UnarmedDif = profStats.Unarmed.BaseValue - ProficiencyBaseValue;
                ShieldDif = profStats.Shield.BaseValue - ProficiencyBaseValue;
                AthleticsDif = profStats.Athletics.BaseValue - ProficiencyBaseValue;
                LightArmorDif = profStats.LightArmor.BaseValue - ProficiencyBaseValue;
                MediumArmorDif = profStats.MediumArmor.BaseValue - ProficiencyBaseValue;
                HeavyArmorDif = profStats.HeavyArmor.BaseValue - ProficiencyBaseValue;
                ArcheryDif = profStats.Archery.BaseValue - ProficiencyBaseValue;
                EvasionDif = profStats.Evasion.BaseValue - ProficiencyBaseValue;
                AcrobaticsDif = profStats.Acrobatics.BaseValue - ProficiencyBaseValue;
                SneakDif = profStats.Sneak.BaseValue - ProficiencyBaseValue;
                TheftDif = profStats.Theft.BaseValue - ProficiencyBaseValue;
                MagicDif = profStats.Magic.BaseValue - ProficiencyBaseValue;
                AlchemyDif = profStats.Alchemy.BaseValue - ProficiencyBaseValue;
                CookingDif = profStats.Cooking.BaseValue - ProficiencyBaseValue;
                HandcraftingDif = profStats.Handcrafting.BaseValue - ProficiencyBaseValue;
                AnimalWeaponDif = profStats.AnimalWeapon.BaseValue - 1;
            }
        }
    }
}
