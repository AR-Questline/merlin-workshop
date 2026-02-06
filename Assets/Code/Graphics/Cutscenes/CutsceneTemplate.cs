using System;
using System.Collections.Generic;
using System.Linq;
using Awaken.TG.Assets;
using Awaken.TG.Main.Stories;
using Awaken.TG.Main.Templates;
using Awaken.TG.MVC;
using Awaken.TG.Utility.Attributes.Tags;
using Awaken.Utility.Collections;
using Awaken.Utility.Debugging;
using UnityEngine;

namespace Awaken.TG.Graphics.Cutscenes {
    public class CutsceneTemplate : ScriptableObject, ITemplate {
        public bool allowSkip;
        public bool stopsStory = true;
        [SerializeField]
        public List<ConditionalCutsceneRef> conditionalCutsceneRefs;
        [SerializeField, ARAssetReferenceSettings(new[] {typeof(GameObject)}, true)]
        ARAssetReference cutsceneRef;
        public SpawnPosition spawnPosition = SpawnPosition.Prefab;

        // === ITemplate
        [SerializeField, HideInInspector] TemplateMetadata metadata;
        public TemplateMetadata Metadata  => metadata;
        public string GUID { get; set; }
        [UnityEngine.Scripting.Preserve] public IEnumerable<ITemplate> DirectAbstracts => Enumerable.Empty<ITemplate>();
        public bool IsAbstract => false;
        
        public ARAssetReference CutsceneView() {
            if (conditionalCutsceneRefs.IsNullOrEmpty()) {
                return cutsceneRef;
            }

            foreach (var conditionalCutsceneRef in conditionalCutsceneRefs) {
                if (string.IsNullOrEmpty(conditionalCutsceneRef.requiredFlag)) {
                    Log.Important?.Error($"No flag for conditional visual prefab in {name}");
                    continue;
                }

                if (StoryFlags.Get(conditionalCutsceneRef.requiredFlag)) {
                    return conditionalCutsceneRef.cutsceneRef;
                }
            }

            return cutsceneRef;
        }

        // === Asset creation
        public static ScriptableObject CreateCutsceneTemplate(string name, GameObject relatedPrefab) {
            CutsceneTemplate template = CreateInstance<CutsceneTemplate>();
            template.name = name;
            return template;
        }

        
        string INamed.DisplayName => string.Empty;
        string INamed.DebugName => name;
        
                
        [Serializable]
        public enum SpawnPosition : byte {
            Prefab,
            Hero,
        }
        
        [Serializable]
        public struct ConditionalCutsceneRef {
            [SerializeField, Tags(TagsCategory.Flag)] 
            public string requiredFlag;
            [SerializeField, ARAssetReferenceSettings(new[] {typeof(GameObject)}, true)]
            public ARAssetReference cutsceneRef;
        }
    }
}