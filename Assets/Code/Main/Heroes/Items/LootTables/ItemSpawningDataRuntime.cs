using Awaken.Utility;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using Awaken.TG.Main.Heroes.Items.Gems;
using Awaken.TG.Main.Heroes.Sketching;
using Awaken.TG.Main.Heroes.Thievery;
using Awaken.TG.Main.NewGamePlus;
using Awaken.TG.Main.Saving;
using Awaken.TG.Main.Saving.LargeFiles;
using Awaken.TG.Main.Skills;
using Awaken.TG.Utility.Attributes;
using Newtonsoft.Json;

namespace Awaken.TG.Main.Heroes.Items.LootTables {
    [Serializable]
    public sealed partial class ItemSpawningDataRuntime {
        public ushort TypeForSerialization => SavedTypes.ItemSpawningDataRuntime;

        [Saved] public ItemTemplate ItemTemplate { get; private set; }
        [Saved(1), DefaultValue(1)] public int quantity = 1;
        [Saved(0)] public int itemLvl;
        [Saved(0)] public int weightLvl;
        [Saved(0)] public int newGamePlusLvl;
        [Saved] public ItemElementsDataRuntime elementsData;

        [JsonConstructor, UnityEngine.Scripting.Preserve]
        ItemSpawningDataRuntime() {
            quantity = 1;
        }

        public ItemSpawningDataRuntime(ItemTemplate itemTemplate) {
            ItemTemplate = itemTemplate;
            itemLvl = itemTemplate.LevelBonus;
            newGamePlusLvl = NewGamePlusSystem.Level;
        }
        
        public ItemSpawningDataRuntime(ItemTemplate itemTemplate, int quantity, int level) {
            ItemTemplate = itemTemplate;
            this.quantity = quantity;
            itemLvl = level;
            newGamePlusLvl = NewGamePlusSystem.Level;
        }
        
        public ItemSpawningDataRuntime(Item item) {
            ItemTemplate = item.Template;
            quantity = item.Quantity;
            itemLvl = item.Level.ModifiedInt;
            weightLvl = item.WeightLevel.ModifiedInt;
            newGamePlusLvl = item.NewGamePlusLevel;
            elementsData = item.TryGetRuntimeData();
        }

        public void TryToRetrieveElements(Item item) {
            if (elementsData == null) {
                return;
            }
            
            if (elementsData.maxSlots > 0) {
                item.AddElement(new ItemGems(elementsData.availableSlots, elementsData.maxSlots));
            }

            foreach (var gem in elementsData.gemData) {
                var gemAttached = new GemAttached(gem.gemTemplate, gem.gemLevel, gem.gemNgPlusLevel, gem.skillReferences);
                item.AddElement(gemAttached);
                foreach (SkillReference skillReference in gemAttached.SkillRefs) {
                    Skill s = skillReference.CreateSkill();
                    gemAttached.AddElement(s);
                }
            }
            
            if (elementsData.crimeData != null) {
                item.AddElement(new StolenItemElement(elementsData.crimeData));
            }

            if (elementsData.sketchIndex != 0) {
                item.AddElement(new Sketch(elementsData.sketchIndex));
            }
            
            if (elementsData.transmogrifiedTemplate != null) {
                item.AddElement(new ItemTransmog(elementsData.transmogrifiedTemplate));
            }
        }
        
        public void WriteSavables(JsonWriter jsonWriter, JsonSerializer serializer) {
            jsonWriter.WriteStartObject();
            JsonUtils.JsonWrite(jsonWriter, serializer, nameof(ItemTemplate), ItemTemplate);
            JsonUtils.JsonWrite(jsonWriter, serializer, nameof(quantity), quantity, 1);
            JsonUtils.JsonWrite(jsonWriter, serializer, nameof(itemLvl), itemLvl);
            JsonUtils.JsonWrite(jsonWriter, serializer, nameof(weightLvl), weightLvl);
            JsonUtils.JsonWrite(jsonWriter, serializer, nameof(newGamePlusLvl), newGamePlusLvl);
            JsonUtils.JsonWrite(jsonWriter, serializer, nameof(elementsData), elementsData);
            jsonWriter.WriteEndObject();
        }
    }
    
    [Serializable]
    public partial class ItemElementsDataRuntime {
        public ushort TypeForSerialization => SavedTypes.ItemElementsDataRuntime;

        // ItemGems
        [Saved] public int availableSlots;
        [Saved] public int maxSlots;

        // GemAttached
        [Saved] public List<GemTemplateWithSkills> gemData = new();
        
        // Thievery
        [Saved] public StolenItemElement.CrimeDataRuntime crimeData;

        // Sketch
        [Saved] public LargeFileIndex sketchIndex;
        
        // Transmogrified
        [Saved] public ItemTemplate transmogrifiedTemplate;
    }

    [Serializable]
    public partial struct GemTemplateWithSkills {
        public ushort TypeForSerialization => SavedTypes.GemTemplateWithSkills;

        [Saved] public ItemTemplate gemTemplate;
        [Saved] public int gemLevel;
        [Saved] public int gemNgPlusLevel;
        [Saved] public List<SkillReference> skillReferences;
    }
}