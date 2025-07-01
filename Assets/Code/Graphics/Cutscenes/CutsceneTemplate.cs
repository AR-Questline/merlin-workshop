using System;
using System.Collections.Generic;
using System.Linq;
using Awaken.TG.Assets;
using Awaken.TG.Main.Templates;
using Awaken.TG.MVC;
using UnityEngine;

namespace Awaken.TG.Graphics.Cutscenes {
    public class CutsceneTemplate : ScriptableObject, ITemplate {
        public bool allowSkip;
        public bool stopsStory = true;
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
    }
}