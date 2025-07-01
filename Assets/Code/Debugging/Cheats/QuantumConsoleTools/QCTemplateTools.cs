using System.Linq;
using Awaken.TG.Debugging.Cheats.QuantumConsoleTools.Suggestors;
using Awaken.TG.Main.AI.Grid;
using Awaken.TG.Main.AI.Utils;
using Awaken.TG.Main.Fights.Mounts;
using Awaken.TG.Main.Fights.NPCs;
using Awaken.TG.Main.Fights.NPCs.Presences;
using Awaken.TG.Main.Heroes;
using Awaken.TG.Main.Heroes.Development.WyrdPowers;
using Awaken.TG.Main.Heroes.Items;
using Awaken.TG.Main.Heroes.Items.Attachments;
using Awaken.TG.Main.Heroes.Statuses;
using Awaken.TG.Main.Heroes.Statuses.Duration;
using Awaken.TG.Main.Locations.Attachments.Elements;
using Awaken.TG.Main.Locations.Pets;
using Awaken.TG.Main.Locations.Setup;
using Awaken.TG.Main.Skills;
using Awaken.TG.Main.Stories.Runtime;
using Awaken.TG.Main.Stories.Steps;
using Awaken.TG.Main.Templates;
using Awaken.TG.MVC;
using Pathfinding;
using QFSW.QC;
using UnityEngine;
using Log = Awaken.Utility.Debugging.Log;

namespace Awaken.TG.Debugging.Cheats.QuantumConsoleTools {
    public static class QCTemplateTools {
        [Command("give-item", "Gives an item to the player", allowWhiteSpaces: true)][UnityEngine.Scripting.Preserve]
        static void GiveItem([TemplateSuggestion(typeof(ItemTemplate))] ItemTemplate itemTemplate, int amount = 1) {
            var hero = Hero.Current;
            if (hero == null) {
                Log.Important?.Error("Hero not found");
                return;
            }
            itemTemplate.ChangeQuantity(hero.Inventory, amount);
        }
        
        [Command("give-any-item", "Gives an item to the player, ignoring template type restrictions", allowWhiteSpaces: true)][UnityEngine.Scripting.Preserve]
        static void GiveAnyItem([TemplateSuggestion(typeof(ItemTemplate), TemplateTypeFlag.All)] ItemTemplate itemTemplate, int amount = 1) {
            var hero = Hero.Current;
            if (hero == null) {
                Log.Important?.Error("Hero not found");
                return;
            }
            itemTemplate.ChangeQuantity(hero.Inventory, amount);
        }

        [Command("spawn-npc", "Spawns an NPC", allowWhiteSpaces:true)][UnityEngine.Scripting.Preserve]
        static void SpawnNpc([NPCName] string templateName, int amount = 1, int spread = 1) {
            LocationTemplate template = World.Services.Get<TemplatesProvider>()
                .GetAllOfType<LocationTemplate>()
                .Where(t => t.gameObject.GetComponent<NpcAttachment>())
                .FirstOrDefault(t => t.name == templateName);
            
            if (template == null) {
                Log.Important?.Error($"LocationTemplateId {templateName} not found");
                return;
            }
            var hero = Hero.Current;
            if (hero == null) {
                Log.Important?.Error("Hero not found");
                return;
            }
            
            for (int i = 0; i < amount; i++) {
                Vector3 localSpawnPoint = new Vector3 {
                    x = 5 * Mathf.Sin(-spread * 5 * i),
                    y = 0,
                    z = i
                };
                var spawnPoint = hero.ActorTransform.TransformPoint(Vector3.forward * 3 + localSpawnPoint);
                template.SpawnLocation(AstarPath.active.GetNearest(spawnPoint).position);
            }
        }
        
        [Command("spawn-companion", "Spawns a gameplay-unique companion (horse, pet)", allowWhiteSpaces: true)]
        static void SpawnCompanion([CompanionName] string templateName) {
            LocationTemplate template = World.Services.Get<TemplatesProvider>()
                .GetAllOfType<LocationTemplate>()
                .Where(t =>
                    t.gameObject.GetComponent<MountAttachment>() ||
                    t.gameObject.GetComponent<PetAttachment>()
                )
                .FirstOrDefault(t => t.name == templateName);
            
            if (template == null) {
                Log.Important?.Error($"LocationTemplateId {templateName} not found");
                return;
            }
            var hero = Hero.Current;
            if (hero == null) {
                Log.Important?.Error("Hero not found");
                return;
            }
            
            var spawnPoint = hero.ActorTransform.TransformPoint(Vector3.forward * 3);
            NNInfo nnInfo = AstarPath.active.GetNearest(spawnPoint);
            if (nnInfo.node == null) {
                Log.Important?.Error("No valid position for companion spawn");
                return;
            }
            var location = template.SpawnLocation(nnInfo.position);
            GameplayUniqueLocation.InitializeForLocation(location);
        }

        [Command("template.load-itemSet", "Adds an item set to the hero's inventory")][UnityEngine.Scripting.Preserve]
        static void LoadItemSet([TemplateSuggestion(typeof(ItemSet))] ItemSet set, bool withEquipping = true, bool ignoreLevelSetting = false, bool withTalents = true, bool withStats = true, bool withWyrdSkill = true) {
            if (set == null) {
                QuantumConsole.Instance.LogToConsoleAsync($"Item set not found");
                return;
            }
            set.ApplyFull(withEquipping, ignoreLevelSetting, withTalents, withStats, withWyrdSkill);
        }

        [Command("template.apply-status.hero", "Applies a status to the player")][UnityEngine.Scripting.Preserve]
        static void ApplyStatusToHero([StatusName] string templateName) {
            var statusTemplate = World.Services
                .Get<TemplatesProvider>()
                .GetAllOfType<StatusTemplate>()
                .FirstOrDefault(t => t.name == templateName);
            
            if (statusTemplate == null) {
                Debug.LogError($"StatusTemplateId {templateName} not found");
                return;
            }
            
            var hero = Hero.Current;
            if (hero == null) {
                Debug.LogError("Hero not found");
                return;
            }

            hero.Statuses.AddStatus(statusTemplate, StatusSourceInfo.FromStatus(statusTemplate));
        }
        
        [Command("template.apply-status.npc", "Applies status to a nearby NPC")] [UnityEngine.Scripting.Preserve]
        static void ApplyStatusToNpc([TemplateSuggestion(typeof(StatusTemplate))] StatusTemplate statusTemplate, float duration = 0.0f) {
            const float SearchDistance = 20f;
            var sourceInfo = StatusSourceInfo.FromStatus(statusTemplate).WithCharacter(Hero.Current);

            var npc = FindNpcUtil.FindClosestToCrosshair(
                    World.Services.Get<NpcGrid>().GetNpcsInSphere(Hero.Current.Coords, SearchDistance), 
                    null, SearchDistance, true, true, true)
                .FirstOrDefault();
            
            if (npc == null) {
                Log.Important?.Error("No NPC found");
                return;
            }

            if (duration == 0.0f) {
                npc.Statuses.AddStatus(statusTemplate, sourceInfo);
            } else {
                npc.Statuses.AddStatus(statusTemplate, sourceInfo, new TimeDuration(duration));
            }
        }
        
        [Command("template.revive-npc", "Revives a target UniqueNPC")] [UnityEngine.Scripting.Preserve]
        static void ReviveNpc([UniqueNPCName] string npcName) {
            LocationTemplate template = World.Services.Get<TemplatesProvider>()
                                             .GetAllOfType<LocationTemplate>()
                                             .Where(t => t.gameObject.GetComponent<UniqueNpcAttachment>())
                                             .FirstOrDefault(t => t.name == npcName);
            if (template == null) {
                QuantumConsole.Instance.LogToConsoleAsync("Npc not found: " + npcName);
                return;
            }

            NpcRegistry.Resurrect(template);
        }
        [Command("hero.unlock-soul.fragment", "Unlocks soul fragment")][UnityEngine.Scripting.Preserve]
        static void UnlockWyrdSoul(WyrdSoulFragmentType soulFragmentType) {
            Hero hero = Hero.Current;
            hero.Development.WyrdSoulFragments.Unlock(soulFragmentType);
        }

        [Command("hero.give-recipe-books", "Gives all recipe books to the player")][UnityEngine.Scripting.Preserve]
        static void GiveAllRecipeBooks() {
            Hero hero = Hero.Current;
            foreach (var template in World.Services.Get<TemplatesProvider>().GetAllOfType<ItemTemplate>()) {
                if (template.TryGetComponent(out ItemReadSpec readSpec) == false) {
                    continue;
                }
                if (readSpec.StoryRef is not { IsSet: true }) {
                    continue;
                }
                var graph = StoryGraphRuntime.Get(readSpec.StoryRef.GUID);
                if (!graph.HasValue) {
                    continue;
                }
                
                if (HasRecipe(graph.Value)) {
                    hero.Inventory.Add(new Item(template));
                }
                graph.Value.Dispose();

                static bool HasRecipe(in StoryGraphRuntime graph) {
                    foreach (var chapter in graph.chapters) {
                        foreach (var step in chapter.steps) {
                            if (step is SLearnRecipe) {
                                return true;
                            }
                        }
                    }
                    return false;
                }
            }
        }
    }
}
