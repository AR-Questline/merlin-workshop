using System;
using System.Collections.Generic;
using Awaken.TG.Assets;
using Awaken.TG.Main.Fights.NPCs;
using Awaken.TG.Main.Locations.Setup;
using Awaken.TG.Main.Templates;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Awaken.TG.Main.General.Caches {
    public class NpcCache : BaseCache, ISceneCache<SceneNpcSources, NpcSource> {
        static NpcCache s_cache;
        public static NpcCache Get => s_cache ??= BaseCache.LoadFromAssets<NpcCache>("3763487f239672147b2e9c8413e36858");

        public List<SceneNpcSources> npcs = new();
        
        public override void Clear() {
            npcs.Clear();
        }

        public bool HasAnyOccurenceOf(NpcTemplate npcTemplate) {
            foreach (var sceneSource in npcs) {
                foreach (var npcSource in sceneSource.data) {
                    if (npcTemplate == npcSource.NpcTemplate) {
                        return true;
                    }
                }
            }

            return false;
        }

        List<SceneNpcSources> ISceneCache<SceneNpcSources, NpcSource>.Data => npcs;
    }

    [Serializable]
    public class SceneNpcSources : SceneDataSources, ISceneCacheData<NpcSource> {
        [PropertyOrder(1), Searchable, ListDrawerSettings(NumberOfItemsPerPage = 8, IsReadOnly = true, ShowFoldout = false, 
             ListElementLabelName = nameof(SceneSource.scenePath))]
        public List<NpcSource> data = new();
        
        public SceneNpcSources(SceneReference sceneRef) : base(sceneRef) { }

        SceneReference ISceneCacheData<NpcSource>.SceneRef => sceneRef;
        List<NpcSource> ISceneCacheData<NpcSource>.Sources => data;
    }

    [Serializable]
    public class NpcSource : SceneSource, ISceneCacheSource, IEquatable<NpcSource> {
        public TemplateReference locationTemplate;
        public TemplateReference npcTemplate;
        
        public LocationTemplate LocationTemplate => locationTemplate?.Get<LocationTemplate>();
        public NpcTemplate NpcTemplate => npcTemplate?.Get<NpcTemplate>();

        public NpcSource(GameObject go, LocationTemplate locationTemplate, NpcTemplate npcTemplate) : base(go) {
            this.locationTemplate = new TemplateReference(locationTemplate);
            this.npcTemplate = new TemplateReference(npcTemplate);
        }

        public bool Equals(NpcSource other) {
            return ReferenceEquals(this, other);
        }
    }
}