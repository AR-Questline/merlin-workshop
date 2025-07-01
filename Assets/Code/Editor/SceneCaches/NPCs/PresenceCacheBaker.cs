using Awaken.TG.Assets;
using Awaken.TG.Editor.SceneCaches.Core;
using Awaken.TG.Editor.Utility.RichLabels;
using Awaken.TG.Editor.Utility.RichLabels.Configs;
using Awaken.TG.Main.Fights.NPCs.Presences;
using Awaken.TG.Main.General.Caches;
using Awaken.TG.Main.Locations.Setup;
using Awaken.TG.Main.Utility.RichLabels.SO;
using UnityEditor;
using UnityEditor.SceneManagement;

namespace Awaken.TG.Editor.SceneCaches.NPCs {
    public class PresenceCacheBaker : SceneBaker<PresenceCache> {
        bool ModifyEmptyPresences => Cache.autoFillEmptyPresencesWithNextBake;
        protected override PresenceCache LoadCache => PresenceCache.Get;

        public override void Bake(SceneReference scene) {
            ScenePresenceSources sources = new(scene);
            ScenePresenceSources modifiedSources = ModifyEmptyPresences ? new(scene) : null;

            foreach (var go in CacheBakerUtils.ForEachSceneGO()) {
                var locationSpec = go.GetComponent<LocationSpec>();
                var npcPresenceAttachment = go.GetComponent<NpcPresenceAttachment>();

                if (!locationSpec || !npcPresenceAttachment) {
                    continue;
                }

                bool wasModified = ModifyEmptyPresences && FillEmptyPresence(locationSpec, npcPresenceAttachment);

                var presenceData = new PresenceSource(locationSpec, npcPresenceAttachment);
                sources.data.Add(presenceData);
                
                if (wasModified) {
                    modifiedSources!.data.Add(presenceData);
                }
            }
            
            if (sources.data.Count > 0) {
                Cache.presences.Add(sources);
            }
            
            if (modifiedSources != null && modifiedSources.data.Count > 0) {
                Cache.modifiedPresences.Add(modifiedSources);
                EditorSceneManager.SaveOpenScenes();
            }
        }

        public override void FinishBaking() {
            Cache.autoFillEmptyPresencesWithNextBake = false;
            base.FinishBaking();
        }

        bool FillEmptyPresence(LocationSpec locationSpec, NpcPresenceAttachment npcPresenceAttachment) {
            var set = npcPresenceAttachment.RichLabelSet;
            if (set.richLabelGuids is { Count: 0 }) {
                npcPresenceAttachment.Editor_Autofill();
                var config = new SerializedObject(RichLabelEditorUtilities.GetOrCreateRichLabelConfig(RichLabelConfigType.Presence));
                var richLabelConfig = (RichLabelConfig)config.targetObject;
                var category = richLabelConfig.RichLabelCategories[0];
                RichLabelEditorUtilities.FillSetCategoryWithLabelsFromTags(set, category, locationSpec);
                EditorUtility.SetDirty(richLabelConfig);
                EditorUtility.SetDirty(locationSpec.gameObject);
                return true;
            }

            return false;
        }
    }
}