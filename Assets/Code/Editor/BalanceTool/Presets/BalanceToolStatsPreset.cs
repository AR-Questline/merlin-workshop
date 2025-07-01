using System;
using System.Collections.Generic;
using System.Linq;
using Awaken.TG.Editor.BalanceTool.Data;
using Awaken.TG.Main.Character;
using Awaken.TG.Main.General.Configs;
using Awaken.TG.Main.General.StatTypes;
using Awaken.TG.Main.Heroes.Items;
using Awaken.TG.Main.Heroes.Setup;
using Awaken.TG.Main.Utility.RichEnums;
using Awaken.Utility.Enums;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Awaken.TG.Editor.BalanceTool.Presets {
    [CreateAssetMenu(menuName = "TG/BalanceTool/StatsPreset")]
    public class BalanceToolStatsPreset : ScriptableObject {
        [SerializeField] public HeroTemplate heroTemplate;
        [SerializeField] public List<StatEntryPreset> baseStatsEntries = new();
        [SerializeField] public List<StatEntryPreset> proficiencyStatsEntries = new();
        [SerializeField] public List<StatEntryPreset> additionalStatsEntries = new();
        [SerializeField] public List<StatEntryPreset> modifiers = new();
        [SerializeField] List<EquipmentEntryPreset> equipmentEntries = new();

        public StatEntryPreset this[StatEntryEnum stat] => additionalStatsEntries.Find(entry => entry.typeName == stat.EnumName);
        
        List<StatEntryPreset> StatsEntries => baseStatsEntries.Concat(proficiencyStatsEntries).Concat(additionalStatsEntries).Concat(modifiers).ToList();

        static readonly List<ProfStatType> AllowedProficiency = new() {
            ProfStatType.OneHanded,
            ProfStatType.TwoHanded,
            ProfStatType.Unarmed,
            ProfStatType.LightArmor,
            ProfStatType.MediumArmor,
            ProfStatType.HeavyArmor,
            ProfStatType.Archery,
            ProfStatType.Magic
        };

        public void CreatePreset(BalanceToolData data) {
            EnsureCorrectStats();
            heroTemplate = data.PlayerTemplate;
            
            foreach (var entry in data.playerEquipment) {
                var valueStats = entry.Value.stats;
                EquipmentEntryPreset equipmentEntry = new(entry.Key, valueStats != null ? valueStats.gameObject : null );
                if (equipmentEntries.Any(e => e.EquipmentSlot == entry.Key)) {
                    equipmentEntries.Remove(equipmentEntries.Find(e => e.EquipmentSlot == entry.Key));
                }
                equipmentEntries.Add(equipmentEntry);
            }

            foreach ((string key, StatEntry statEntry) in data.AllEntries()) {
                StatEntryPreset statEntryPreset = StatsEntries.Find(entry => entry.typeName == key);
                
                if(statEntryPreset == null) {
                    continue;
                }

                StatsPreset statsPreset = new(statEntry.name, statEntry.description, statEntry.BaseValue, statEntry.Modifiers, statEntry.AddPerLevel);
                statEntryPreset.stat = statsPreset;
            }
        }
        
        public GameObject GetEquipmentTemplate(EquipmentSlotType slot) {
            EquipmentEntryPreset entry = equipmentEntries.Find(entry => entry.EquipmentSlot == slot);
            return entry?.itemStats ? entry?.itemStats : null;
        }
        
        [Button]
        void EnsureCorrectStats() {
            foreach (var stat in RichEnum.AllValuesOfType<HeroRPGStatType>()) {
                if (baseStatsEntries.Any(entry => entry.typeName == stat.EnumName)) {
                    continue;
                }
                
                StatsPreset entry = new(stat.DisplayName, stat.Description, GameConstants.Get.RPGStatParamsByType[stat].InnateStatLevel);
                baseStatsEntries.Add(new(stat.EnumName, entry));
            }

            foreach (var stat in RichEnum.AllValuesOfType<ProfStatType>().Where(stat => AllowedProficiency.Contains(stat))) {
                if (proficiencyStatsEntries.Any(entry => entry.typeName == stat.EnumName)) {
                    continue;
                }
                
                StatsPreset entry = new(stat.DisplayName, stat.Description, ProficiencyStats.ProficiencyBaseValue);
                proficiencyStatsEntries.Add(new(stat.EnumName, entry));
            }
            
            foreach (var stat in RichEnum.AllValuesOfType<AdditionalStatEntryEnum>()) {
                if (additionalStatsEntries.Any(entry => entry.typeName == stat.EnumName)) {
                    continue;
                }
                
                StatsPreset entry = new(stat.statEntry.name, stat.statEntry.description, stat.statEntry.BaseValue, stat.statEntry.AddPerLevel);
                additionalStatsEntries.Add(new(stat.EnumName, entry));
            }
            
            foreach (var stat in RichEnum.AllValuesOfType<ModifiersStatEntryEnum>()) {
                if (modifiers.Any(entry => entry.typeName == stat.EnumName)) {
                    continue;
                }
                
                StatsPreset entry = new(stat.statEntry.name, stat.statEntry.description, stat.statEntry.BaseValue, stat.statEntry.Modifiers, stat.statEntry.AddPerLevel);
                modifiers.Add(new(stat.EnumName, entry));
            }
        }
    }

    [Serializable]
    public class EquipmentEntryPreset {
        [SerializeField, RichEnumExtends(typeof(EquipmentSlotType))] RichEnumReference type;
        public GameObject itemStats;

        public EquipmentSlotType EquipmentSlot => type.EnumAs<EquipmentSlotType>();
        
        public EquipmentEntryPreset(EquipmentSlotType slot, GameObject item) {
            type = slot;
            itemStats = item;
        }
    }
    
    [Serializable]
    public class StatEntryPreset {
        public string typeName;
        public StatsPreset stat;
        
        public StatEntryPreset(string name, StatsPreset stat) {
            this.typeName = name;
            this.stat = stat;
        }
    }
    
    [Serializable]
    public class StatsPreset {
        public string name;
        public string description;
        public float baseValue;
        public float modifiers;
        public float addPerLevel;
        
        public StatsPreset(string name, string description, float baseValue) {
            SetPreset(name, description, baseValue);
        }
        
        public StatsPreset(string name, string description, float baseValue, float addPerLevel) {
            SetPreset(name, description, baseValue);
            this.addPerLevel = addPerLevel;
        }
        
        public StatsPreset(string name, string description, float baseValue, float modifiers, float addPerLevel) {
            SetPreset(name, description, baseValue);
            this.modifiers = modifiers;
            this.addPerLevel = addPerLevel;
        }
        
        void SetPreset(string name, string description, float baseValue) {
            this.name = name;
            this.description = description;
            this.baseValue = baseValue;
        }
    }
}
