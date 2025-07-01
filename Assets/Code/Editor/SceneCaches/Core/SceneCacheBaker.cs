using Awaken.TG.Assets;
using Awaken.TG.Editor.SceneCaches.Items;
using Awaken.TG.Editor.SceneCaches.Locations;
using Awaken.TG.Editor.SceneCaches.NPCs;
using Awaken.TG.Editor.SceneCaches.Quests;
using Awaken.TG.Editor.SceneCaches.Scenes;
using Awaken.TG.Editor.Utility;
using Awaken.TG.Main.General.Caches;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Awaken.TG.Editor.SceneCaches.Core {
    public static class SceneCacheBaker {
        static readonly ISceneBaker[] Bakers = {
            new LocationCacheBaker(),
            new LootCacheBaker(),
            new ItemsInGameCacheBaker(),
            new NpcCacheBaker(),
            new EncountersCacheBaker(),
            new QuestCacheBaker(),
            new PresenceCacheBaker()
        };

        public static void Bake() {
            using var buildBaking = new BuildSceneBaking();
            
            foreach (var baker in Bakers) {
                baker.StartBaking();
            }
            
            SceneReference initialScene = SceneReference.ByAddressable(new ARAssetReference("a8cfefa6f4b68e34295decd0d4104edb")); // Prologue_Jail

            foreach (var sceneRef in SceneCrawler.CrawlAllScenes(initialScene)) {
                foreach (var baker in Bakers) {
                    baker.Bake(sceneRef);
                }
            }
            
            foreach (var baker in Bakers) {
                baker.FinishBaking();
            }
            
            Resources.UnloadUnusedAssets();
        }

        [MenuItem("TG/Build/Baking/Bake Scene Cache For Current Scene", false, 101)]
        public static void BakeCurrentScene() {
            using var buildBaking = new BuildSceneBaking();
            
            foreach (var baker in Bakers) {
                baker.StartBaking();
            }

            string scenePath = SceneManager.GetActiveScene().path;
            string sceneGuid = AssetDatabase.AssetPathToGUID(scenePath);
            SceneReference sceneRef = SceneReference.ByAddressable(new ARAssetReference(sceneGuid));
            foreach (var baker in Bakers) {
                baker.Bake(sceneRef);
            }
            
            foreach (var baker in Bakers) {
                baker.FinishBaking();
            }
            
            Resources.UnloadUnusedAssets();
        }
    }
}