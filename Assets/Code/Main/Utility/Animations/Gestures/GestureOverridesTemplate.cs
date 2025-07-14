using Awaken.TG.Assets;
using Awaken.TG.Main.Templates;
using Awaken.TG.MVC;
using Awaken.Utility.Collections;
using UnityEngine;

namespace Awaken.TG.Main.Utility.Animations.Gestures {
    public class GestureOverridesTemplate : ScriptableObject, ITemplate {
        public GestureOverrides gestureOverrides;

        public GestureData? TryToGetAnimationClipRef(string key) {
            return gestureOverrides.TryToGetAnimationClipRef(key);
        }
        
        // === ITemplate
        [SerializeField, HideInInspector] TemplateMetadata metadata;
        public TemplateMetadata Metadata => metadata;
        public string GUID { get; set; }
        public PooledList<ITemplate> DirectAbstracts => PooledList<ITemplate>.Empty;
        public bool IsAbstract => false;
        string INamed.DisplayName => string.Empty;
        string INamed.DebugName => name;
    }
}