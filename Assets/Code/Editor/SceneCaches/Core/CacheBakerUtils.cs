using System;
using System.Collections.Generic;
using System.Linq;
using Awaken.TG.Assets;
using Awaken.TG.Editor.SceneCaches.Items;
using Awaken.TG.Editor.SceneCaches.Locations;
using Awaken.TG.Main.AI.Combat.Behaviours.CustomBehaviours;
using Awaken.TG.Main.AI.Idle.Interactions;
using Awaken.TG.Main.Fights.Factions.Crimes;
using Awaken.TG.Main.Fights.NPCs;
using Awaken.TG.Main.Heroes.Items.Attachments;
using Awaken.TG.Main.Locations.Actions.Attachments;
using Awaken.TG.Main.Locations.Actions.Customs;
using Awaken.TG.Main.Locations.Clearing;
using Awaken.TG.Main.Locations.Spawners;
using Awaken.TG.Main.Scenes.SceneConstructors;
using Awaken.TG.Main.Stories;
using Awaken.TG.Main.Stories.Core;
using Awaken.TG.Main.Utility.Animations.FightingStyles;
using Awaken.Utility.Debugging;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Awaken.TG.Editor.SceneCaches.Core {
    public static class CacheBakerUtils {
        public static IEnumerable<GameObject> ForEachSceneGO() {
            foreach (var go in Object.FindObjectsByType<Transform>(FindObjectsInactive.Exclude, FindObjectsSortMode.None)) {
                yield return go.gameObject;
            }
        }
        
        public static IEnumerable<(GameObject sceneGO, GameObject projectGO)> ForEachProjectGO(SceneReference sceneRef) {
            foreach (var go in ForEachSceneGO()) {
                yield return (go, go);
            }

            // Now we need to extract spawned locations from the scene (from cache)
            if (!LocationCache.Get.HasBakedScene(sceneRef)) {
                Log.Important?.Error("Used ForEachProjectGO before location bake, it will not include locations.");
            }

            foreach (var location in LocationCache.Get.GetAllSpawnedLocations(sceneRef)) {
                GameObject go = location.SceneGameObject;
                yield return (go, location.SpawnedLocationTemplate.gameObject);
            }
        }
        
        /// <summary>
        /// Should return all stories that might be executed on that scene. 
        /// </summary>
        public static IEnumerable<(StoryBookmark, GameObject)> ForEachStory(SceneReference sceneRef) {
            // TODO: Add Fore-dweller talks by the bonfire
            
            foreach (var (sceneGO, projectGO) in ForEachProjectGO(sceneRef)) {
                foreach (var (bookmark, _) in ForEachStoryIn(projectGO)) {
                    yield return (bookmark, sceneGO);
                }
            }

            // Extract graphs from readable items
            SceneItemSources sceneItemSources = LootCache.Get.sceneSources.FirstOrDefault(s => s.sceneRef == sceneRef);
            if (sceneItemSources == null) {
                Log.Important?.Error($"ForEachStory invoked before items bake, it will not include readables. Scene: {sceneRef.Name}");
            } else {
                foreach (var itemSource in sceneItemSources.sources) {
                    foreach (var loot in itemSource.lootData) {
                        var readSpec = loot.Template?.GetComponent<ItemReadSpec>();
                        if (readSpec != null && StoryBookmark.ToInitialChapter(readSpec.StoryRef, out var bookmark)) {
                            yield return (bookmark, itemSource.SceneGameObject);
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Checks only components on give game object, doesn't check if f.e. location spawns another location
        /// </summary>
        public static IEnumerable<(StoryBookmark, Component)> ForEachStoryIn(GameObject go) {
            if (go.TryGetComponent(out MapScene mapScene)) {
                yield return (mapScene.initialStory, mapScene);
            }

            if (go.TryGetComponent(out DialogueAttachment dialogue)) {
                yield return (dialogue.bookmark, dialogue);
            }

            if (go.TryGetComponent(out StoryInteraction interaction)) {
                yield return (interaction.Bookmark, interaction);
            }

            if (go.TryGetComponent(out StoryInteractAttachment storyInteract)) {
                yield return (storyInteract.storyBookmark, storyInteract);
            }

            if (go.TryGetComponent(out ReadAttachment read)) {
                if (StoryBookmark.ToInitialChapter(read.StoryRef, out var bookmark)) {
                    yield return (bookmark, read);
                }
            }

            if (go.TryGetComponent(out PickItemAttachment pickItem)) {
                yield return (pickItem.storyBookmark, pickItem);
            }
            
            if (go.TryGetComponent(out SearchStoryAttachment searchStory)) {
                yield return (searchStory.story, searchStory);
            }

            if (go.TryGetComponent(out SpawnerAttachment spawner)) {
                yield return (spawner.storyOnAllKilled, spawner);
            }

            if (go.TryGetComponent(out StartStoryOnConditionAttachment condition)) {
                yield return (condition.Story, condition);
            }

            if (go.TryGetComponent(out NpcAttachment npc)) {
                StoryGraph deathStory = null;

                try {
                    deathStory = npc.StoryOnDeath?.Get<StoryGraph>();
                } catch (Exception e) {
                    Log.Minor?.Error($"Exception happened for {go.name} on {go.scene.name}");
                    Debug.LogException(e);
                }

                if (deathStory != null) {
                    yield return (StoryBookmark.EDITOR_ToInitialChapter(deathStory), npc);
                }

                if (npc.NpcTemplate.CrimeReactionArchetype == CrimeReactionArchetype.Guard) {
                    var guardStory = npc.NpcTemplate.FightingStyle?.BaseBehaviours?.Get().EditorLoad<AREnemyBehavioursMapping>()
                        ?.CombatBehaviours?.OfType<GuardIntervention>().FirstOrDefault()?.GuardStory;
                    if (guardStory != null) {
                        yield return (guardStory, npc);
                    }
                }
            }

            if (go.TryGetComponent(out StonehengeAttachment stonehenge)) {
                var story = stonehenge.storyRef;
                if (story.IsValid) {
                    yield return (story, stonehenge);
                }
            }
        }
    }
}