using System;
using System.Collections.Generic;
using Awaken.TG.Main.Templates;
using UnityEngine;

namespace Awaken.TG.Main.Utility.Video
{
    [Serializable, CreateAssetMenu(fileName = "VideoSetData", menuName = "TG/Video/VideoSetData")]
    public class VideoSetData : ScriptableObject, ITemplate {
        [SerializeField] ConditionalVideo[] videos = Array.Empty<ConditionalVideo>();
        
        public LoadingHandle[] GetLoadingHandles() {
            var result = new List<LoadingHandle>();

            foreach (ConditionalVideo t in videos) {
                if (t.ShouldPlay()) {
                    result.Add(t.Video);
                }
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
