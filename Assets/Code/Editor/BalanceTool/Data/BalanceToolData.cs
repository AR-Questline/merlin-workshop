using System;
using System.Collections.Generic;
using System.Linq;
using Awaken.TG.Editor.BalanceTool.Presets;
using Awaken.TG.Main.General.StatTypes;
using Awaken.TG.Main.Heroes.Items;
using Awaken.TG.Main.Heroes.Items.Attachments;
using Awaken.TG.Main.Heroes.Setup;
using UnityEditor.UIElements;

namespace Awaken.TG.Editor.BalanceTool.Data {
    public class BalanceToolData {
        public Dictionary<string, StatEntry> baseStatsEntries = new();
        public Dictionary<string, StatEntry> proficiencyStatsEntries = new();
        public Dictionary<string, StatEntry> additionalStatsEntries = new();
        public Dictionary<string, StatEntry> modifiers = new();
        
        public HeroTemplate PlayerTemplate { get; private set; }
        public readonly Dictionary<EquipmentSlotType, (ObjectField field, ItemStatsAttachment stats)> playerEquipment = new() {
            { EquipmentSlotType.MainHand, default },
            { EquipmentSlotType.Helmet, default },
            { EquipmentSlotType.Cuirass, default },
            { EquipmentSlotType.Gauntlets, default },
            { EquipmentSlotType.Greaves, default },
            { EquipmentSlotType.Boots, default },
            { EquipmentSlotType.Back, default }
        };
        
        public StatEntry this[HeroRPGStatType stat] => baseStatsEntries[stat.EnumName];
        public StatEntry this[ProfStatType stat] => proficiencyStatsEntries[stat.EnumName];
        public StatEntry this[StatEntryEnum stat] => AllEntries()[stat.EnumName];
        public Dictionary<string, StatEntry> this[StatEntryType entryType] => GetEntries(entryType);

        public (ObjectField field, ItemStatsAttachment stats) this[EquipmentSlotType slot] {
            get => playerEquipment[slot];
            set => playerEquipment[slot] = value;
        }
        
        public void SetPlayerTemplate(HeroTemplate template) {
            PlayerTemplate = template;
        }

        public void SetEquipment(EquipmentSlotType slot, ObjectField field) {
            playerEquipment[slot] = (field, null);
        }
        
        public void UpdateEntries() {
            foreach (var stat in AllEntries().Values) {
                stat.UpdateEffective();
            }
        }
        
        public void OverrideEntries(List<StatEntryPreset> entries) {
            var allEntries = AllEntries();
            foreach (var stat in entries) {
                allEntries[stat.typeName].OverrideStat(stat);
            }
        }
        
        public void AddEntries(List<StatEntryPreset> entries, StatEntryType entryType) {
            Dictionary<string, StatEntry> statsEntries = this[entryType];
            
            foreach (var stat in entries) {
                statsEntries.Add(stat.typeName, new StatEntry(stat));
            }
        }
        
        public Dictionary<string, StatEntry> AllEntries() {
            Dictionary<string, StatEntry> statsEntries = new();
            
            foreach (StatEntryType entryType in Enum.GetValues(typeof(StatEntryType))) {
                statsEntries = statsEntries.Concat(this[entryType]).ToDictionary(pair => pair.Key, pair => pair.Value);
            }
            
            return statsEntries;
        }

        Dictionary<string, StatEntry> GetEntries(StatEntryType entryType) {
            return entryType switch {
                StatEntryType.Base => baseStatsEntries,
                StatEntryType.Proficiency => proficiencyStatsEntries,
                StatEntryType.Additional => additionalStatsEntries,
                StatEntryType.Modifiers => modifiers,
                _ => throw new ArgumentOutOfRangeException(nameof(entryType), entryType, null)
            };
        }
    }
    
    public enum StatEntryType {
        Base,
        Proficiency,
        Additional,
        Modifiers
    }
}
