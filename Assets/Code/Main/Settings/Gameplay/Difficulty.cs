using System;
using Awaken.TG.Main.Localization;
using Awaken.TG.Utility;
using Awaken.Utility.Enums;

namespace Awaken.TG.Main.Settings.Gameplay {
    public class Difficulty : RichEnum {
        readonly string _name;
        readonly string _desc;

        public string Name => _name.Translate();
        public string Description => _desc.Translate();
        public float DamageDealt { get; }
        public float DamageReceived { get; }
        public float StaminaUsage { get; }
        public float ManaUsage { get; }
        public int MaxEnemiesAttacking { get; }
        public float AttackActionUnBookProlong { get; }
        
        public SaveRestriction SaveRestriction { get; }

        protected Difficulty(string enumName, string name, string description, float damageDealt, float damageReceived,
            float staminaUsage, float manaUsage, int maxEnemiesAttacking, float attackActionUnBookProlong,
            SaveRestriction restrictions = SaveRestriction.None) : base(enumName, nameof(Difficulty)) {
            _name = name;
            _desc = description;
            DamageDealt = damageDealt;
            DamageReceived = damageReceived;
            StaminaUsage = staminaUsage;
            ManaUsage = manaUsage;
            MaxEnemiesAttacking = maxEnemiesAttacking;
            AttackActionUnBookProlong = attackActionUnBookProlong;
            SaveRestriction = restrictions;
        }

        [UnityEngine.Scripting.Preserve] 
        public static readonly Difficulty
            Story = new(nameof(Story), LocTerms.DifficultyStory, LocTerms.DifficultyStoryDesc, 3, 0.1f, 0.5f, 0.5f, 1, 0.5f),
            Easy = new(nameof(Easy), LocTerms.DifficultyEasy, LocTerms.DifficultyEasyDesc, 2, 0.66f, 0.8f, 0.8f, 1, 0.375f),
            Normal = new(nameof(Normal), LocTerms.DifficultyNormal, LocTerms.DifficultyNormalDesc, 1, 1, 1, 1, 2, 0.25f),
            Hard = new(nameof(Hard), LocTerms.DifficultyHard, LocTerms.DifficultyHardDesc, 0.8f, 1.5f, 1, 1, 3, 0.125f),
            Survival = new(nameof(Survival), LocTerms.DifficultySurvival, LocTerms.DifficultySurvivalDesc, 0.8f, 1.75f, 1.1f, 1.05f, 4, 0.1f, SaveRestriction.SurvivalSaving);
    }
    
    [Flags]
    public enum SaveRestriction : byte {
        None,
        SurvivalSaving = 1 << 0,
        OneSaveSlot = 1 << 1,
        Hardcore = 1 << 2
    }
}