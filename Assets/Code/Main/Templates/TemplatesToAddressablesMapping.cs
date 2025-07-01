using Awaken.TG.Main.AI.Idle.Data.Attachment;
using Awaken.TG.Main.Crafting;
using Awaken.TG.Main.Crafting.Recipes;
using Awaken.TG.Main.Fights.NPCs;
using Awaken.TG.Main.Heroes.Items;
using Awaken.TG.Main.Heroes.Items.LootTables;
using Awaken.TG.Main.Heroes.Statuses;
using Awaken.TG.Main.Locations.Setup;
using Awaken.TG.Main.Locations.Shops;
using Awaken.TG.Main.Skills;
using Awaken.TG.Main.Stories.Core;
using Awaken.TG.Main.Stories.Quests.Templates;
using UnityEngine;

namespace Awaken.TG.Main.Templates {
    public class TemplatesToAddressablesMapping {
        public const string AddressableGroupItems = "Templates.Items";
        public const string AddressableGroupLocations = "Templates.Locations";
        public const string AddressableGroupStatuses = "Templates.Statuses";
        public const string AddressableGroupStory = "Templates.Story";
        public const string AddressableGroupSkills = "Templates.Skills";
        public const string AddressableGroupQuests = "Templates.Quests";
        public const string AddressableGroupLoot = "Templates.Loot";
        public const string AddressableGroupNpc = "Templates.Npc";
        public const string AddressableGroupCrafting = "Templates.Crafting";
        public const string AddressableGroupDefault = "Templates";

        public static readonly string[] ValidAddressableGroups = {
            AddressableGroupItems,
            AddressableGroupLocations,
            AddressableGroupStatuses,
            AddressableGroupStory,
            AddressableGroupSkills,
            AddressableGroupQuests,
            AddressableGroupLoot,
            AddressableGroupNpc,
            AddressableGroupCrafting,
            AddressableGroupDefault,
        };
        
        public static string AddressableGroup(object obj) {
            if (obj is GameObject go) {
                obj = go.GetComponent<ITemplate>();
            } else if (obj is not ITemplate && obj is Component component) {
                obj = component.GetComponent<ITemplate>();
            }
            
            return obj switch {
                ItemTemplate => AddressableGroupItems,
                LocationTemplate => AddressableGroupLocations,
                StatusTemplate => AddressableGroupStatuses,
                StoryGraph => AddressableGroupStory,
                SkillGraph => AddressableGroupSkills,
                QuestTemplateBase => AddressableGroupQuests,
                
                LootTableAsset => AddressableGroupLoot,
                ILootTable => AddressableGroupLoot,

                NpcTemplate => AddressableGroupNpc,
                IdleDataTemplate => AddressableGroupNpc,
                ShopTemplate => AddressableGroupNpc,
                
                CraftingTemplate => AddressableGroupCrafting,
                IRecipe => AddressableGroupCrafting,
                
                ITemplate => AddressableGroupDefault,
                _ => null,
            };
        }
    }
}