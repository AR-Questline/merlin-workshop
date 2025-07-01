using System;
using System.Collections.Generic;
using System.Linq;
using Awaken.TG.Assets;
using Awaken.Utility.Collections;

namespace Awaken.TG.Main.General.Caches {
    public class ScenesCache : BaseCache {
        static ScenesCache s_cache;
        public static ScenesCache Get => s_cache ??= BaseCache.LoadFromResources<ScenesCache>("Caches/ScenesCache");
        
        public List<SceneRegion> regions = new();
        
        public SceneReference TryGetMainSceneOfTheSubscene(SceneReference sceneRef) {
            return regions.FirstOrDefault(r => r.subscenes.Contains(sceneRef))?.regionScene;
        }
        
        public SceneReference GetOpenWorldRegion(SceneReference sceneRef) {
            return regions.FirstOrDefault(r => r.All.Contains(sceneRef))?.regionScene;
        }

        public bool IsOpenWorld(SceneReference sceneRef) {
            return regions.Any(r => r.regionScene == sceneRef);
        }

        public override void Clear() {
            regions.Clear();
        }
    }

    [Serializable]
    public class SceneRegion {
        public SceneReference regionScene;
        public List<SceneReference> subscenes;
        public List<SceneReference> dungeons;
        public List<SceneReference> interiors;
        
        public SceneRegion(SceneReference regionScene) {
            this.regionScene = regionScene;
            subscenes = new List<SceneReference>();
            dungeons = new List<SceneReference>();
            interiors = new List<SceneReference>();
        }

        public IEnumerable<SceneReference> All => regionScene.Yield().Concat(subscenes).Concat(dungeons).Concat(interiors);
    }
}