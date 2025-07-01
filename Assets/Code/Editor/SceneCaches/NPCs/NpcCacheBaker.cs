using Awaken.TG.Assets;
using Awaken.TG.Editor.SceneCaches.Core;
using Awaken.TG.Editor.SceneCaches.Locations;
using Awaken.TG.Main.Fights.NPCs;
using Awaken.TG.Main.General.Caches;
using Awaken.TG.Main.Locations.Setup;
using Awaken.TG.Main.Templates;
using Awaken.Utility.Debugging;
using UnityEditor;
using UnityEngine;

namespace Awaken.TG.Editor.SceneCaches.NPCs {
    public class NpcCacheBaker : SceneBaker<NpcCache> {
        protected override NpcCache LoadCache => NpcCache.Get;

        public override void Bake(SceneReference scene) {
            SceneNpcSources sources = new(scene);
            
            foreach (var location in LocationCache.Get.GetAllLocations(scene)) {
                GameObject go = location.SceneGameObject;
                if (go == null) {
                    Log.Important?.Error($"{scene.Name} - {location.scenePath}");
                }

                if (location.IsSpawned && location.SpawnedLocationTemplate == null) {
                    Log.Important?.Error($"{scene.Name} - {location.scenePath} - {location.spawnedLocationTemplate.GUID} is spawned but has no spawned location template.");
                }
                NpcAttachment npc = location.IsSpawned
                    ? location.SpawnedLocationTemplate.GetComponent<NpcAttachment>()
                    : go.GetComponent<NpcAttachment>();

                LocationTemplate locationTemplate = location.IsSpawned
                    ? location.SpawnedLocationTemplate
                    : RetrieveTemplate(go);
                
                if (npc != null) {
                    if (locationTemplate != null) {
                        var npcSource = new NpcSource(go, locationTemplate, npc.NpcTemplate);
                        sources.data.Add(npcSource);
                    } else {
                        Log.Important?.Error($"{scene.Name} - {location.scenePath} has no location template.");
                    }
                }
            }
            
            if (sources.data.Count > 0) {
                Cache.npcs.Add(sources);
            }
        }
        
        LocationTemplate RetrieveTemplate(GameObject go) {
            string path = PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(go);
            if (path == null) {
                return null;
            }
            
            LocationTemplate template = AssetDatabase.LoadAssetAtPath<LocationTemplate>(path);
            if (template != null) {
                TemplatesUtil.EDITOR_AssignGuid(template, template.gameObject);
            }
            return template;
        }
    }
}