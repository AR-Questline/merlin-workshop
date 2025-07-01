using System;
using System.Collections.Generic;
using System.Linq;
using Awaken.TG.Assets;
using Awaken.TG.Main.Fights.NPCs;
using Awaken.TG.Main.General.Caches;
using Awaken.TG.Main.Locations.Discovery;
using Awaken.TG.Main.Locations.Setup;
using Awaken.TG.Main.Stories.Actors;
using Awaken.TG.Main.Stories.Tags;
using Awaken.TG.Main.Templates;
using Awaken.Utility.Extensions;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Awaken.TG.Editor.SceneCaches.Locations {
    public class LocationCache : BaseCache, ISceneCache<SceneLocationSources, LocationSource> {
        static LocationCache s_cache;
        public static LocationCache Get => s_cache ??= BaseCache.LoadFromAssets<LocationCache>("2312cfc7e9c8395488d1c155bfaaaa13");

        public List<SceneLocationSources> locations = new();
        
        public override void Clear() {
            locations.Clear();
        }
        
        public bool HasBakedScene(SceneReference sceneRef) => locations.Any(l => l.sceneRef == sceneRef);

        public IEnumerable<LocationSource> GetAllLocations(SceneReference sceneRef) {
            return locations
                .Where(l => l.sceneRef == sceneRef)
                .SelectMany(l => l.data);
        }

        public IEnumerable<LocationSource> GetAllSpawnedLocations(SceneReference sceneRef) {
            return locations
                .Where(l => l.sceneRef == sceneRef)
                .SelectMany(l => l.data)
                .Where(l => l.IsSpawned);
        }

        public bool HasAnyOccurrencesOf(LocationTemplate locationTemplate) {
            foreach (var sceneSource in locations) {
                foreach (var locationSource in sceneSource.data) {
                    if (locationSource.locationTemplate == locationTemplate) {
                        return true;
                    }

                    if (locationSource.IsSpawned && locationSource.SpawnedLocationTemplate == locationTemplate) {
                        return true;
                    }
                }
            }
            
            return false;
        }
        
        List<SceneLocationSources> ISceneCache<SceneLocationSources, LocationSource>.Data => locations;
    }

    [Serializable]
    public class SceneLocationSources : SceneDataSources, ISceneCacheData<LocationSource> {
        [PropertyOrder(1), Searchable, ListDrawerSettings(NumberOfItemsPerPage = 8, IsReadOnly = true, ShowFoldout = false, 
             ListElementLabelName = nameof(LocationSource.scenePath))]
        public List<LocationSource> data = new();

        public SceneLocationSources(SceneReference sceneRef) : base(sceneRef) { }
        
        SceneReference ISceneCacheData<LocationSource>.SceneRef => sceneRef;
        List<LocationSource> ISceneCacheData<LocationSource>.Sources => data;
    }

    [Serializable]
    public class LocationSource : SceneSource, ITagged, ISceneCacheSource, IEquatable<LocationSource> {
        public TemplateReference spawnedLocationTemplate;
        public LocationTemplate locationTemplate;
        public int spawnAmount = 1;
        public BitMaskIndex bitMask;

        [ListDrawerSettings(DefaultExpandedState = true, IsReadOnly = true)]
        public string[] tags;

        public string actorGuid;

        [ShowInInspector] public string ActorName => ActorsRegister.Get.Editor_GetActorName(actorGuid);

        public ICollection<string> Tags => tags;

        public bool Respawns {
            get => bitMask.HasFlagFast(BitMaskIndex.Respawns);
            set => Set(ref bitMask, BitMaskIndex.Respawns, value);
        }
        public bool OnlyNight {
            get => bitMask.HasFlagFast(BitMaskIndex.OnlyNight);
            set => Set(ref bitMask, BitMaskIndex.OnlyNight, value);
        }
        public bool Conditional {
            get => bitMask.HasFlagFast(BitMaskIndex.Conditional);
            set => Set(ref bitMask, BitMaskIndex.Conditional, value);
        }

        public bool IsSpawned => spawnedLocationTemplate is { IsSet : true };
        public LocationTemplate SpawnedLocationTemplate => spawnedLocationTemplate?.Get<LocationTemplate>();

        public LocationSource(GameObject go, LocationTemplate locationTemplate, LocationTemplate template) : this(go,locationTemplate, template.GetComponent<LocationSpec>()) { }

        public LocationSource(GameObject go, LocationTemplate locationTemplate, LocationSpec spec) : base(go) {
            this.locationTemplate = locationTemplate;
            tags = ExtractTags(spec).ToArray();
            if (spec.TryGetComponent(out NpcAttachment npc)) {
                actorGuid = npc.Editor_GetActorForCache().guid;
            }
        }

        static IEnumerable<string> ExtractTags(LocationSpec spec) {
            foreach (var tag in spec.tags) {
                yield return tag;
            }

            foreach (var discovery in spec.GetComponentsInChildren<LocationDiscoveryAttachment>(true)) {
                yield return discovery.UnlockFlag;
            }
        }

        static void Set(ref BitMaskIndex value, BitMaskIndex flag, bool set) {
            value = set ? value | flag : value & ~flag;
        }

        [Flags]
        public enum BitMaskIndex : byte {
            Respawns = 1 << 0,
            OnlyNight = 1 << 1,
            Conditional = 1 << 2,
        }

        public bool Equals(LocationSource other) {
            return ReferenceEquals(this, other);
        }
    }
}