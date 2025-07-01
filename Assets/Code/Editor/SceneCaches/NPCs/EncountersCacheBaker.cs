using System.Collections.Generic;
using System.Linq;
using Awaken.TG.Assets;
using Awaken.TG.Editor.Assets.Templates;
using Awaken.TG.Editor.SceneCaches.Core;
using Awaken.TG.Editor.SceneCaches.Locations;
using Awaken.TG.Editor.Utility;
using Awaken.TG.Main.Fights.Factions;
using Awaken.TG.Main.General.Caches;
using Awaken.TG.Main.Locations.Discovery;
using Awaken.TG.Main.Locations.Setup;
using UnityEditor;
using UnityEngine;

namespace Awaken.TG.Editor.SceneCaches.NPCs {
    public class EncountersCacheBaker : SceneBaker<EncountersCache> {
        const float InteriorEncounterDistance = 8f;
        const float ExteriorEncounterDistance = 15f;

        protected override EncountersCache LoadCache => EncountersCache.Get;

        FactionTree _tree;
        Faction _heroFaction;

        public override void StartBaking() {
            base.StartBaking();
            _tree = new(TemplatesSearcher.FindAllOfType<FactionTemplate>().ToArray());

            FactionTemplate heroFactionTemplate = AssetDatabase.LoadAssetAtPath<FactionTemplate>(AssetDatabase.GUIDToAssetPath("f541926e32941de40ad78cd27c3d7f99"));
            _heroFaction = _tree.FactionByTemplate(heroFactionTemplate);
        }

        public override void Bake(SceneReference scene) {
            List<EnemyWithPos> enemiesWithPosition = new();
            List<EncounterData> encounters = new();

            // Gather all enemies
            var sceneNpcs = NpcCache.Get.npcs.FirstOrDefault(npc => npc.sceneRef == scene)?.data ?? Enumerable.Empty<NpcSource>();
            foreach (var npc in sceneNpcs) {
                var factionTemplate = npc.NpcTemplate.FactionEditorContext;
                if (factionTemplate != null) {
                    var npcFaction = _tree.FactionByTemplate(factionTemplate);
                    if (npcFaction.IsHostileTo(_heroFaction)) {
                        Vector3 transformPosition = npc.SceneGameObject.transform.position;
                        float difficultyScore = npc.NpcTemplate.DifficultyScore;
                        enemiesWithPosition.Add(new(npc, transformPosition, difficultyScore));
                    }
                }
            }

            bool isInterior = BuildTools.GetSceneConfigs().IsOpenWorld(scene) == false;

            // Group them in encounters
            while (enemiesWithPosition.Count > 0) {
                var npcWithPos = enemiesWithPosition[0];
                enemiesWithPosition.RemoveAt(0);

                EncounterData encounter = new();
                encounter.npcs.Add(npcWithPos);

                var whereabouts = FindClosestFastTravelPoint(encounter.npcs[0].pos, scene);
                if (string.IsNullOrEmpty(whereabouts)) {
                    whereabouts = scene.Name;
                }
                encounter.Whereabouts = whereabouts;
                FindCloseEnemies(enemiesWithPosition, encounter, isInterior);
                encounters.Add(encounter);
            }

            if (encounters.Any()) {
                encounters.Sort((a, b) => b.DifficultyScore.CompareTo(a.DifficultyScore));
                Cache.encounters.Add(new SceneEncountersSources(scene) {
                    data = encounters
                });
            }
        }

        static string FindClosestFastTravelPoint(Vector3 pos, SceneReference scene) {
            string closestLocationName = null;
            float closestDistance = float.MaxValue;
            foreach (var location in LocationCache.Get.GetAllLocations(scene)) {
                var discoveryAttachment = location.SceneGameObject.GetComponent<LocationDiscoveryAttachment>();
                var locationSpec = location.SceneGameObject.GetComponent<LocationSpec>();
                if (!discoveryAttachment || !locationSpec || !discoveryAttachment.IsFastTravel) {
                    continue;
                }
                
                Vector3 locationPos = discoveryAttachment.FastTravelLocation;
                float newDistance = Vector3.SqrMagnitude(locationPos - pos);
                if (newDistance < closestDistance) {
                    closestDistance = newDistance;
                    closestLocationName = locationSpec.displayName;
                }
            }

            return closestLocationName;
        }

        static void FindCloseEnemies(List<EnemyWithPos> enemiesWithPosition, EncounterData encounter, bool isInterior) {
            bool anyChange = false;
            for (int j = 0; j < enemiesWithPosition.Count; j++) {
                var npcWithPos2 = enemiesWithPosition[j];
                if (encounter.npcs.Any(npc => IsCloseEnough(npc.pos, npcWithPos2.pos, isInterior))) {
                    anyChange = true;
                    encounter.npcs.Add(npcWithPos2);
                    enemiesWithPosition.RemoveAt(j);
                    j--;
                }
            }

            if (anyChange) {
                FindCloseEnemies(enemiesWithPosition, encounter, isInterior);
            }
        }

        static bool IsCloseEnough(Vector3 a, Vector3 b, bool isInterior) {
            if (isInterior) {
                return Vector3.Distance(a, b) < InteriorEncounterDistance;
            } else {
                return Vector3.Distance(a, b) < ExteriorEncounterDistance;
            }
        }
    }
}