using System.Collections.Generic;
using Awaken.TG.Assets;
using Awaken.TG.Editor.SceneCaches.Locations;
using Awaken.TG.Main.Fights.NPCs;

namespace Awaken.TG.Editor.EnemyCounter {
    public static class SceneEnemySearcher {
        public static List<EnemyCounterWindow.ResultRow> SearchEnemiesInScene(SceneReference sceneToSearchIn) {
            var tempResults = new Dictionary<string, int>();

            var sceneName = sceneToSearchIn.RetrieveName();
            
            foreach (var location in LocationCache.Get.locations) {
                foreach (var source in location.data) {
                    if (IsMatching(sceneName, source)) {
                        if (!tempResults.TryAdd(source.locationTemplate.name, source.spawnAmount)) {
                            tempResults[source.locationTemplate.name] += source.spawnAmount;
                        }
                    }
                }
            }
            
            var results = new List<EnemyCounterWindow.ResultRow>(tempResults.Count);
            foreach (var kvp in tempResults) {
                results.Add(new EnemyCounterWindow.ResultRow(kvp.Key, kvp.Value));
            }
            
            results.Sort((a, b) => string.Compare(a.key, b.key, System.StringComparison.Ordinal));
            return results;
        }
        
        static bool IsMatching(string searchSceneName, LocationSource source) {
            if (source.locationTemplate == null) {
                return false;
            }

            if (source.locationTemplate.GetComponent<NpcAttachment>() == null) {
                return false;
            }
            
            return searchSceneName == source.SceneName;
        }
    }
}