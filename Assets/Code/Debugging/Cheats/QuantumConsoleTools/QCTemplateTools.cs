using System.Collections.Generic;
using System.Linq;
using Awaken.TG.Debugging.Cheats.QuantumConsoleTools.Suggestors;
using Awaken.TG.Main.AI.Grid;
using Awaken.TG.Main.AI.States;
using Awaken.TG.Main.AI.Utils;
using Awaken.TG.Main.Fights.Mounts;
using Awaken.TG.Main.Fights.NPCs;
using Awaken.TG.Main.Fights.NPCs.Presences;
using Awaken.TG.Main.Grounds;
using Awaken.TG.Main.Heroes;
using Awaken.TG.Main.Heroes.Development.WyrdPowers;
using Awaken.TG.Main.Heroes.Items;
using Awaken.TG.Main.Heroes.Items.Attachments;
using Awaken.TG.Main.Heroes.Items.LootTables;
using Awaken.TG.Main.Heroes.Statuses;
using Awaken.TG.Main.Heroes.Statuses.Duration;
using Awaken.TG.Main.Locations;
using Awaken.TG.Main.Locations.Attachments.Elements;
using Awaken.TG.Main.Locations.Pets;
using Awaken.TG.Main.Locations.Setup;
using Awaken.TG.Main.NewGamePlus;
using Awaken.TG.Main.Skills;
using Awaken.TG.Main.Stories.Runtime;
using Awaken.TG.Main.Stories.Steps;
using Awaken.TG.Main.Templates;
using Awaken.TG.Main.Utility.Debugging;
using Awaken.TG.MVC;
using Pathfinding;
using QFSW.QC;
using Unity.Mathematics;
using UnityEngine;
using Log = Awaken.Utility.Debugging.Log;

namespace Awaken.TG.Debugging.Cheats.QuantumConsoleTools {
    public static class QCTemplateTools {
        [Command("give-item", "Gives an item to the player", allowWhiteSpaces: true)][UnityEngine.Scripting.Preserve]
        static void GiveItem([TemplateSuggestion(typeof(ItemTemplate))] ItemTemplate itemTemplate, int amount = 1, int ngPlusLevel = -1) {
            var hero = Hero.Current;
            if (hero == null) {
                Log.Important?.Error("Hero not found");
                return;
            }

            if (amount > 0 && ngPlusLevel >= 0) {
                var itemData = new ItemSpawningDataRuntime(itemTemplate) {
                    quantity = amount,
                    itemLvl = NewGamePlusSystem.CalculateBonusItemLevelValue(ngPlusLevel) + itemTemplate.LevelBonus,
                    newGamePlusLvl = ngPlusLevel
                };
                hero.Inventory.AddItems(itemData);
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

        [Command("logging.get-npc-info", "Logs information about current state of a NPC", allowWhiteSpaces: true)][UnityEngine.Scripting.Preserve]
        static void GetInfoAboutNPC([NPCName] string templateName) {
            LocationTemplate template = World.Services.Get<TemplatesProvider>()
                .GetAllOfType<LocationTemplate>()
                .Where(t => t.gameObject.GetComponent<NpcAttachment>())
                .FirstOrDefault(t => t.name == templateName);
            
            if (template == null) {
                Log.Important?.Error($"LocationTemplateId {templateName} not found");
                return;
            }

            NpcElement npcElement = null;
            foreach (var npc in World.All<NpcElement>()) {
                if (template.Equals(npc.ParentModel.Template)) {
                    npcElement = npc;
                    break;
                }
            }

            if (npcElement == null) {
                Log.Important?.Error($"Npc for LocationTemplateId {templateName} not found");
                return;
            }
            Log.Important?.Error($"Npc for LocationTemplateId {templateName} found {LogUtils.GetDebugName(npcElement)}");

            if (npcElement.IsUnique) {
                int useAmountInUniqueNpcStash = World.Services.Get<UniqueNpcStash>().GetUseAmount(npcElement);
                string presenceName = npcElement.NpcPresence?.ParentModel.DebugName ?? "N/A";
                string additionalPresenceInfo = "";
                foreach (var presence in World.All<NpcPresence>()) {
                    if (presence.Template.Equals(template)) {
                        additionalPresenceInfo += $"- {LogUtils.GetDebugName(presence.ParentModel)}: Position {presence.ParentModel.Coords} Available: {presence.Available} Attached: {presence.Attached}\n";
                    }
                }
                Log.Important?.Error($"Npc {templateName} Unique Info: Current Presence {presenceName}, UniqueNpcStash Use Amount {useAmountInUniqueNpcStash}\n{additionalPresenceInfo}");
            }
            
            bool movementHasController = npcElement.Movement?.Controller != null;
            string mainState = npcElement.NpcAI?.Behaviour?.CurrentState.GetType().ToString();
            string workingState = npcElement.NpcAI?.Behaviour?.CurrentState is StateAIWorking aiWorking ? aiWorking.CurrentState?.GetType().ToString() : "N/A";
            string currentInteraction = npcElement.Interactor?.CurrentInteraction?.ToString() ?? "N/A";
            Log.Important?.Error($"Npc {templateName} Info: Position: {npcElement.Coords}, VisualLoaded {npcElement.ParentModel.IsVisualLoaded}, CompletelyInitialized {npcElement.HasCompletelyInitialized}\n" +
                                 $"HasController {movementHasController}, MainState {mainState}, WorkingState {workingState}, CurrentInteraction {currentInteraction}\n" +
                                 $"IdleStack: {npcElement.Behaviours?.CurrentStackInfo}");
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
        
#if AR_DEBUG || DEBUG
        static readonly Queue<Location> SpawnedLocationInstances = new();
        static readonly Queue<int> SpawnLocationBatchSizes = new();

        [Command("location.spawn-on-ground", "Spawns location template on the ground in front of the player with specified count and spread")]
        [UnityEngine.Scripting.Preserve]
        static void SpawnLocationOnGround([TemplateSuggestion(typeof(LocationTemplate), TemplateTypeFlag.All)] LocationTemplate locationTemplate, int count = 1, float spread = 5f) {
            SpawnLocationInternal(locationTemplate, count, spread, false);
        }

        [Command("location.spawn-elevated", "Spawns location template 20cm above player's feet in XZ plane with specified count and spread")]
        [UnityEngine.Scripting.Preserve]
        static void SpawnLocationElevated([TemplateSuggestion(typeof(LocationTemplate), TemplateTypeFlag.All)] LocationTemplate locationTemplate, int count = 1, float spread = 5f) {
            SpawnLocationInternal(locationTemplate, count, spread, true);
        }

        [Command("location.despawn-last", "Despawns the last spawned location batch")]
        [UnityEngine.Scripting.Preserve]
        static void DespawnLastLocation() {
            if (SpawnedLocationInstances.Count == 0 || SpawnLocationBatchSizes.Count == 0) {
                QuantumConsole.Instance.LogToConsoleAsync("No spawned location batches to despawn");
                return;
            }

            int lastBatchSize = SpawnLocationBatchSizes.Dequeue();

            int actualDespawned = 0;
            for (int i = 0; i < lastBatchSize && SpawnedLocationInstances.Count > 0; i++) {
                var lastInstance = SpawnedLocationInstances.Dequeue();

                if (lastInstance is { HasBeenDiscarded: false }) {
                    lastInstance.Discard();
                }
                actualDespawned++;
            }

            QuantumConsole.Instance.LogToConsoleAsync($"Despawned last batch of {actualDespawned} location instances. {SpawnedLocationInstances.Count} remaining");
        }

        [Command("location.despawn-all", "Despawns all spawned location instances")]
        [UnityEngine.Scripting.Preserve]
        static void DespawnAllLocations() {
            int totalCount = SpawnedLocationInstances.Count;

            while (SpawnedLocationInstances.Count > 0) {
                var instance = SpawnedLocationInstances.Dequeue();
                if (instance is { HasBeenDiscarded: false }) {
                    instance.Discard();
                }
            }

            SpawnLocationBatchSizes.Clear();

            QuantumConsole.Instance.LogToConsoleAsync($"Despawned all {totalCount} location instances");
        }

        static void SpawnLocationInternal(LocationTemplate locationTemplate, int count, float spread, bool elevated) {
            var hero = Hero.Current;
            if (hero == null) {
                QuantumConsole.Instance.LogToConsoleAsync("Hero not found");
                return;
            }

            if (locationTemplate == null) {
                QuantumConsole.Instance.LogToConsoleAsync("Location template not found");
                return;
            }

            var heroPosition = hero.Coords;
            var heroForward = hero.ActorTransform.forward;

            SpawnLocationInstancesWithSpread(locationTemplate, heroPosition, heroForward, count, spread, !elevated);
            SpawnLocationBatchSizes.Enqueue(count);

            QuantumConsole.Instance.LogToConsoleAsync($"Spawned {count} location instances at {(elevated ? "elevated" : "ground")} level with {spread}m spread");
        }

        static void SpawnLocationInstancesWithSpread(LocationTemplate locationTemplate, Vector3 basePosition, Vector3 forward, int count, float spread, bool shouldSnapToGround) {
            if (count == 1) {
                var spawnPosition = basePosition + forward * 2f;
                spawnPosition = GetLocationPosition(spawnPosition, shouldSnapToGround);
                SpawnSingleLocationInstance(locationTemplate, spawnPosition, forward);
                return;
            }

            int cols = Mathf.CeilToInt(Mathf.Sqrt(count));
            int rows = Mathf.CeilToInt((float)count / cols);
            var right = Vector3.Cross(forward, Vector3.up).normalized;
            float spacing = spread / math.max(cols - 1, 1);
            float startX = -(cols - 1) * spacing * 0.5f;
            float startZ = 2f;

            int spawnedCount = 0;
            for (int row = 0; row < rows && spawnedCount < count; row++) {
                for (int col = 0; col < cols && spawnedCount < count; col++) {
                    float xOffset = startX + col * spacing;
                    float zOffset = startZ + row * spacing;
                    var spawnPosition = basePosition + right * xOffset + forward * zOffset;
                    spawnPosition = GetLocationPosition(spawnPosition, shouldSnapToGround);
                    SpawnSingleLocationInstance(locationTemplate, spawnPosition, forward);
                    spawnedCount++;
                }
            }
        }

        static void SpawnSingleLocationInstance(LocationTemplate locationTemplate, Vector3 position, Vector3 forward) {
            try {
                var location = locationTemplate.SpawnLocation(position, Quaternion.LookRotation(forward));
                
                if (location != null) {
                    SpawnedLocationInstances.Enqueue(location);
                } else {
                    Log.Important?.Warning($"Failed to spawn location '{locationTemplate.name}' at position {position}");
                }
            } catch (System.Exception ex) {
                Log.Critical?.Error($"Failed to spawn location: {ex.Message}");
            }
        }
        
        static Vector3 GetLocationPosition(Vector3 position, bool shouldSnapToGround) {
            return shouldSnapToGround 
                ? Ground.SnapToGround(position + new Vector3(0f, 100, 0f)) 
                : position + new Vector3(0f, 0.2f, 0f);
        }
#endif
    }
}
