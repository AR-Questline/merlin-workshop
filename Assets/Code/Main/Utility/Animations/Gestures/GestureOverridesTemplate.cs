using System.Collections.Generic;
using System.Linq;
using Awaken.TG.Main.Locations.Attachments;
using Awaken.TG.Main.Templates;
using Awaken.TG.MVC;
using Awaken.Utility.Collections;
using UnityEngine;

namespace Awaken.TG.Main.Utility.Animations.Gestures {
    public class GestureOverridesTemplate : ScriptableObject, ITemplate, IContainerAsset<GestureOverrides> {
        public GestureOverrides gestureOverrides;

        public AnimationClip TryToGetAnimationClip(string key) {
            return gestureOverrides.TryToGetAnimationClip(key);
        }
        
        // === ITemplate
        [SerializeField, HideInInspector] TemplateMetadata metadata;
        public TemplateMetadata Metadata => metadata;
        public string GUID { get; set; }
        public PooledList<ITemplate> DirectAbstracts => PooledList<ITemplate>.Empty;
        public bool IsAbstract => false;
        public GestureOverrides Container => gestureOverrides;
        string INamed.DisplayName => string.Empty;
        string INamed.DebugName => name;
    }
}