using System.Collections.Generic;
using System.Linq;
using Awaken.TG.Assets;
using Awaken.TG.Editor.SceneCaches.Core;
using Awaken.TG.Main.Fights.Mounts;
using Awaken.TG.Main.Fights.NPCs.Presences;
using Awaken.TG.Main.Heroes.Housing;
using Awaken.TG.Main.Locations;
using Awaken.TG.Main.Locations.Actions.Customs;
using Awaken.TG.Main.Locations.Setup;
using Awaken.TG.Main.Locations.Spawners;
using Awaken.TG.Main.Templates;
using Awaken.Utility.Collections;
using UnityEditor;
using UnityEngine;

namespace Awaken.TG.Editor.SceneCaches.Locations {
    public class LocationCacheBaker : SceneBaker<LocationCache> {
        protected override LocationCache LoadCache => LocationCache.Get;

        public override void Bake(SceneReference scene) {
            var sceneLocations = new SceneLocationSources(scene);

            foreach (var locationSource in ForEachLocation()) {
                sceneLocations.data.Add(locationSource);
            }

            Cache.locations.Add(sceneLocations);
        }

        static IEnumerable<LocationSource> ForEachLocation() {
            foreach (var go in CacheBakerUtils.ForEachSceneGO()) {
                LocationTemplate locationTemplate = RetrieveTemplate(go);
                
                if (go.TryGetComponent(out LocationSpec spec)) {
                    yield return new LocationSource(go, locationTemplate, spec) {
                        Conditional = spec.StartInteractability != LocationInteractability.Active
                    };
                }
                
                if (go.TryGetComponent(out NpcPresenceAttachment presence)) {
                    var presenceLocationTemplate = presence.Template;
                    if (presenceLocationTemplate != null) {
                        yield return new LocationSource(go, locationTemplate, presenceLocationTemplate) {
                            spawnedLocationTemplate = new TemplateReference(presenceLocationTemplate),
                            Conditional = true,
                        };
                    }
                }

                if (go.TryGetComponent(out GroupSpawnerAttachment groupSpawner)) {
                    var locationsToSpawn = groupSpawner.LocationsToSpawn
                        .Select(l => l.LocationToSpawn)
                        .WhereNotNull();

                    foreach (var location in locationsToSpawn) {
                        yield return new LocationSource(go, locationTemplate, location) {
                            spawnedLocationTemplate = new TemplateReference(location),
                            OnlyNight = groupSpawner.spawnOnlyAtNight,
                            Respawns = !groupSpawner.discardAfterSpawn && !groupSpawner.discardAfterAllKilled,
                            Conditional = groupSpawner.GetComponent<LocationSpec>()?.StartInteractability != LocationInteractability.Active
                        };
                    }
                }

                if (go.TryGetComponent(out LocationSpawnerAttachment locationSpawner)) {
                    var locationsToSpawn = locationSpawner.LocationsToSpawn.WhereNotNull();

                    foreach (var location in locationsToSpawn) {
                        yield return new LocationSource(go, locationTemplate, location) {
                            spawnedLocationTemplate = new TemplateReference(location),
                            spawnAmount = locationSpawner.spawnAmount,
                            OnlyNight = locationSpawner.spawnOnlyAtNight,
                            Respawns = !locationSpawner.discardAfterSpawn && !locationSpawner.discardAfterAllKilled,
                            Conditional = locationSpawner.GetComponent<LocationSpec>()?.StartInteractability != LocationInteractability.Active
                        };
                    }
                }

                if (go.TryGetComponent(out MountSpawnerAttachment mountSpawner)) {
                    var template = mountSpawner.Template;
                    if (template != null) {
                        yield return new LocationSource(go, locationTemplate, template) {
                            spawnedLocationTemplate = new TemplateReference(template),
                        };
                    }
                }

                if (go.TryGetComponent(out StonehengeAttachment stonehenge)) {
                    var template = stonehenge.Druid1;
                    if (template != null) {
                        yield return new LocationSource(go, locationTemplate, template) {
                            spawnedLocationTemplate = new TemplateReference(template),
                            Conditional = true,
                        };
                    }

                    template = stonehenge.Druid2;
                    if (template != null) {
                        yield return new LocationSource(go, locationTemplate, template) {
                            spawnedLocationTemplate = new TemplateReference(template),
                            Conditional = true,
                        };
                    }
                }
            }
        }
        
        static LocationTemplate RetrieveTemplate(GameObject go) {
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