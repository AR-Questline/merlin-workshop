using Awaken.TG.Main.Templates;
using Awaken.TG.MVC;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Awaken.TG.Main.Maps.Markers {
    public interface IMarkerDataTemplate : ITemplate {
        MarkerData MarkerData { get; }
    }
    
    public abstract class MarkerDataTemplate<TData> : ScriptableObject, IMarkerDataTemplate where TData : MarkerData {
#if UNITY_EDITOR
        [InfoBox("Empty Guid!", InfoMessageType.Error, nameof(HasEmptyGuid))]
        [InfoBox("Not my Guid!", InfoMessageType.Error, nameof(HasNotMyGuid))]
        [InlineButton(nameof(RefreshGuid), "Refresh", ShowIf = nameof(ShouldRefreshGuid))]
#endif
        [SerializeField] string guid;
        public string GUID { 
            get => guid;
            set { }
        }
        
        [SerializeField, HideInInspector] TemplateMetadata metadata;
        public TemplateMetadata Metadata => metadata;
        
        [HideLabel, SerializeField] TData markerData;
        
        public TData Get => markerData;
        MarkerData IMarkerDataTemplate.MarkerData => markerData;

        string INamed.DisplayName => string.Empty;
        string INamed.DebugName => name;
#if UNITY_EDITOR
        bool HasEmptyGuid => string.IsNullOrEmpty(guid);
        bool HasNotMyGuid => HasEmptyGuid == false && guid != ValidGuid;
        bool ShouldRefreshGuid => HasEmptyGuid || HasNotMyGuid;
        string ValidGuid => UnityEditor.AssetDatabase.AssetPathToGUID(UnityEditor.AssetDatabase.GetAssetPath(this));
        
        void Reset() {
            RefreshGuid();
        }

        void RefreshGuid() {
            guid = ValidGuid;
        }
#endif
        
    }
}