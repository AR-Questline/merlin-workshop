using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Awaken.TG.Main.Stories.Core;
using Awaken.TG.Main.Templates;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Awaken.TG.Main.AI.Barks {
    [Serializable]
    public class BarkConfig {
        [SerializeField, TemplateType(typeof(StoryGraph))]
        public TemplateReference storyRef;

        public bool HasStory => storyRef != null && storyRef.IsSet;
        public TemplateReference StoryRef => storyRef;

#if UNITY_EDITOR
        StoryGraph _storyCache;
        static string[] s_allBookmarks;

        IEnumerable<string> AllBookmarks => s_allBookmarks ??= typeof(BarkBookmarks)
            .GetFields(BindingFlags.Public | BindingFlags.Static | BindingFlags.FlattenHierarchy)
            .Where(fi => fi.IsLiteral && !fi.IsInitOnly && fi.FieldType == typeof(string))
            .Select(fi => (string)fi.GetRawConstantValue())
            .ToArray();
        [ShowInInspector, DontValidate] 
        string[] Implemented => HasStoryGraph ? AllBookmarks.Intersect(EDITOR_GetStory().BookmarkNames).ToArray() : Array.Empty<string>();

        [ShowInInspector, DontValidate] 
        string[] NotYetImplemented => HasStoryGraph ? AllBookmarks.Except(EDITOR_GetStory().BookmarkNames).ToArray() : Array.Empty<string>();

        [DontValidate]
        bool HasStoryGraph => EDITOR_GetStory(null) != null;

        [DontValidate]
        public StoryGraph EDITOR_GetStory(object debugTarget = null) {
            if (_storyCache == null || _storyCache.GUID != storyRef.GUID) {
                storyRef.TryGet(out _storyCache, debugTarget);
            }

            return _storyCache;
        }
#endif
    }
}