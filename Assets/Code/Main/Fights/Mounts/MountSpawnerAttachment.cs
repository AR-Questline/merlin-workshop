using System;
using Awaken.TG.Assets;
using Awaken.TG.Graphics.Previews;
using Awaken.TG.Main.Locations.Attachments;
using Awaken.TG.Main.Locations.Setup;
using Awaken.TG.Main.Templates;
using Awaken.TG.Main.Templates.Attachments;
using Awaken.TG.MVC.Elements;
using Awaken.Utility.Debugging;
using UnityEngine;

namespace Awaken.TG.Main.Fights.Mounts {
    [ExecuteAlways, SelectionBase]
    [AttachesTo(typeof(LocationSpec), AttachmentCategory.Technical, "Used by horses, required for cross-scene travels.")]
    public class MountSpawnerAttachment : MonoBehaviour, IAttachmentSpec, IPrefabHandleToPreview {
        [SerializeField] TemplateReference templateRef;

        public LocationTemplate Template => templateRef.Get<LocationTemplate>();
        
        public void SetLocation(TemplateReference template) {
            this.templateRef = template;
        }
        
        public Element SpawnElement() {
            return new MountSpawner();
        }

        public bool IsMine(Element element) {
            return element is MountSpawner;
        }

        // === IPrefabHandleToPreview
#if UNITY_EDITOR
        void OnEnable() {
            EDITOR_RegisterPreview();
        }

        void OnDisable() {
            EDITOR_UnregisterPreview();
        }
#endif
        
        public GameObject PreviewParent => gameObject;
        string IWithRenderersToPreview.DisablePreviewKey => "disableMountPreviews";
        bool IPrefabHandleToPreview.TryLoadPrefabToPreview(out ARAsyncOperationHandle<GameObject> handle) {
            return TryLoadPrefab(out handle);
        }
        
        void EDITOR_RegisterPreview() => ((IWithRenderersToPreview)this).RegisterToPreview();
        void EDITOR_UnregisterPreview() => ((IWithRenderersToPreview)this).UnregisterFromPreview();
        
        bool TryLoadPrefab(out ARAsyncOperationHandle<GameObject> handle) {
            if (TryGetValidPrefab(out var prefab)) {
                try {
                    handle = prefab.LoadAsset<GameObject>();
                    return true;
                } catch (Exception e) {
                    LogInvalidPrefab(e.Message);
                    Debug.LogException(e, this);
                }
            }
            handle = default;
            return false;
        }
        
        bool TryGetValidPrefab(out ARAssetReference prefab) {
            try {
                if (templateRef?.IsSet ?? false) {
                    prefab = Template.GetComponent<LocationSpec>().prefabReference;
                    if (prefab is { IsSet: true }) {
                        return true;
                    }

                    return false;
                }
            } catch(Exception e) {
                LogInvalidPrefab(e.Message);
                Debug.LogException(e, this);
            }
            prefab = null;
            return false;
        }
        
        void LogInvalidPrefab(string message) {
            Log.Important?.Error($"MountSpawnerAttachment <color=#FFA43B>{gameObject.name}</color> has Mount with invalid prefab reference.\n{message}", this);
        }
    }
}
