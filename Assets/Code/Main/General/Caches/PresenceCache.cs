using System;
using System.Collections.Generic;
using System.Linq;
using Awaken.TG.Assets;
using Awaken.TG.Main.Fights.NPCs.Presences;
using Awaken.TG.Main.Locations;
using Awaken.TG.Main.Locations.Discovery;
using Awaken.TG.Main.Locations.Setup;
using Awaken.TG.Main.Utility.RichLabels;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Awaken.TG.Main.General.Caches {
    public class PresenceCache : BaseCache, ISceneCache<ScenePresenceSources, PresenceSource> {
        static PresenceCache s_cache;
        public static PresenceCache Get => s_cache = s_cache ? s_cache : LoadFromResources<PresenceCache>("Caches/PresenceCache");

        public List<ScenePresenceSources> presences = new();
        
        // Modified Presences list is overriden with each bake, if we modify it only once than the data won't be lost.
        public bool autoFillEmptyPresencesWithNextBake;
        public List<ScenePresenceSources> modifiedPresences = new();

        public override void Clear() {
            presences.Clear();
        }

        public PresenceSource[] GetMatchingPresenceData(LocationReference location) {
            return presences.SelectMany(s => s.data).Where(p => IsMatchingLocationReference(p, location)).ToArray();

            bool IsMatchingLocationReference(PresenceSource presenceSource, LocationReference locationReference) {
                // This is for handling previous approach to presence activation, when only tags had sense.
                return locationReference.tags.All(presenceSource.tags.Contains);
            }
        }

        public PresenceSource[] GetMatchingPresenceData(RichLabelUsageEntry[] richLabelGuids) {
            return presences.SelectMany(s => s.data).Where(p => RichLabelUtilities.IsMatchingRichLabel(p.richLabelSet, richLabelGuids)).ToArray();
        }

        List<ScenePresenceSources> ISceneCache<ScenePresenceSources, PresenceSource>.Data => presences;
    }

    [Serializable]
    public class ScenePresenceSources : SceneDataSources, ISceneCacheData<PresenceSource> {
        [PropertyOrder(1)]
        [ListDrawerSettings(NumberOfItemsPerPage = 8, IsReadOnly = true, ShowFoldout = false, ListElementLabelName = nameof(PresenceSource.label))]
        public List<PresenceSource> data = new();

        public ScenePresenceSources(SceneReference sceneRef) : base(sceneRef) { }

        SceneReference ISceneCacheData<PresenceSource>.SceneRef => sceneRef;
        List<PresenceSource> ISceneCacheData<PresenceSource>.Sources => data;
    }

    [Serializable]
    public class PresenceSource : SceneSource, ISceneCacheSource, IEquatable<PresenceSource> {
        [HideInInspector] public string label;
        public string[] tags;
        public RichLabelSet richLabelSet;
        
        public PresenceSource(LocationSpec locationSpec, NpcPresenceAttachment npcPresenceAttachment) : base(locationSpec.gameObject) {
            label = locationSpec.gameObject.name;
            tags = ExtractTags(locationSpec).ToArray();
            richLabelSet = new RichLabelSet(npcPresenceAttachment.RichLabelSet);
        }

        IEnumerable<string> ExtractTags(LocationSpec spec) {
            foreach (var tag in spec.tags) {
                yield return tag;
            }

            foreach (var discovery in spec.GetComponentsInChildren<LocationDiscoveryAttachment>(true)) {
                yield return discovery.UnlockFlag;
            }
        }

        public bool Equals(PresenceSource other) {
            return ReferenceEquals(this, other);
        }
    }
}