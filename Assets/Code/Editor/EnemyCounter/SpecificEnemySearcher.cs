using System.Collections.Generic;
using Awaken.TG.Editor.SceneCaches.Locations;
using Awaken.TG.Main.Locations;
using Awaken.TG.Main.Stories.Tags;

namespace Awaken.TG.Editor.EnemyCounter {
    public static class SpecificEnemySearcher {
        public static List<EnemyCounterWindow.ResultRow> SearchSpecificEnemyAcrossScenes(LocationReference locationToFind) {
            var tempResults = new Dictionary<string, int>();
            
            foreach (var location in LocationCache.Get.locations) {
                foreach (var source in location.data) {
                    if (IsMatching(locationToFind, source)) {
                        if (!tempResults.TryAdd(source.SceneName, source.spawnAmount)) {
                            tempResults[source.SceneName] += source.spawnAmount;
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

        static bool IsMatching(LocationReference reference, LocationSource source) {
            if (reference.TargetsActors) {
                foreach (var actor in reference.actors) {
                    if (actor.guid == source.actorGuid) {
                        return true;
                    }
                }
                return false;
            }

            if (reference.TargetsTags) {
                if (reference.targetTypes == TargetType.Tags) {
                    return TagUtils.HasRequiredTags(source, reference.tags);
                }

                return TagUtils.HasAnyTag(source.Tags, reference.tags);
            }

            if (reference.TargetsTemplates) {
                var template = source.locationTemplate;
                var spawnedTemplate = source.SpawnedLocationTemplate;
                
                if (template != null) {
                    foreach (var refTemplate in reference.LocationTemplates) {
                        if (refTemplate == template) {
                            return true;
                        }
                    }
                }
                
                if (spawnedTemplate != null) {
                    foreach (var refTemplate in reference.LocationTemplates) {
                        if (refTemplate == spawnedTemplate) {
                            return true;
                        }
                    }
                }
            }

            return false;
        }
    }
}