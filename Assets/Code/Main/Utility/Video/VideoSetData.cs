using System;
using System.Collections.Generic;
using Awaken.TG.Main.Templates;
using Sirenix.OdinInspector;
using UnityEngine;

#if !UNITY_GAMECORE && !UNITY_PS5
using Awaken.TG.Main.Analytics;
#endif

namespace Awaken.TG.Main.Utility.Video
{
    [Serializable, CreateAssetMenu(fileName = "VideoSetData", menuName = "TG/Video/VideoSetData")]
    public class VideoSetData : ScriptableObject, ITemplate {
        [SerializeField] ConditionalVideo[] videos = Array.Empty<ConditionalVideo>();
        [InfoBox("Analytics prefix will be used to send analytics about which videos were played. " +
                 "\nIf you don't need analytics, leave it empty." +
                 "\nIf you need analytics, use X:Y:Z format, e.q Custom:Ending:Finals")]
        [SerializeField] string analyticsPrefix;
        
        bool IsAnalyticsPrefixEmpty => string.IsNullOrWhiteSpace(analyticsPrefix);
        
        public LoadingHandle[] GetLoadingHandles() {
            var result = new List<LoadingHandle>();
            bool useAnalytics = !IsAnalyticsPrefixEmpty;
            List<int> selectedIDs = useAnalytics ? new List<int>() : null;

            for (int i = 0; i < videos.Length; i++) {
                if (videos[i].ShouldPlay()) {
                    result.Add(videos[i].Video);
                    if (useAnalytics) {
                        selectedIDs.Add(i);
                    }
                }
            }

            if (useAnalytics) {
#if !UNITY_GAMECORE && !UNITY_PS5
                HeroAnalytics.TrySendVideoSetEvent(videos, selectedIDs, analyticsPrefix);
#endif
            }

            return result.ToArray();
        }

        [SerializeField, HideInInspector] TemplateMetadata metadata;
        
        public string DisplayName => string.Empty;
        public string DebugName => name;
        public TemplateMetadata Metadata => metadata;
        public string GUID { get; set; }
    }
}
