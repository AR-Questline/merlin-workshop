using System;
using Awaken.TG.Assets;
using Awaken.TG.Graphics.Previews;
using Awaken.TG.Main.Fights.Factions;
using Awaken.TG.Main.Heroes.Interactions;
using Awaken.TG.Main.Heroes.Items;
using Awaken.TG.Main.Heroes.Items.LootTables;
using Awaken.TG.Main.Locations.Actions.Attachments;
using Awaken.TG.Main.Locations.Setup;
using Awaken.TG.Main.Templates;
using Awaken.TG.Main.Templates.Specs;
using Awaken.TG.Main.Utility.Availability;
using Awaken.Utility.Debugging;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Awaken.TG.Main.Locations.Pickables {
    [ExecuteAlways, SelectionBase, LabelWidth(150)]
    public abstract class PickableSpecBase : SceneSpec, IInteractableWithHeroProvider, IPrefabHandleToPreview {
        [DetailedInfoBox(
@"Represents pickable item without any more logic. It does not support any of the following:",
@"Represents pickable item without any more logic. It does not support any of the following:
  - when you want to change its visibility
  - when you want to handle ItemPicked event in VisualScripting
  - when you want to reference it from story or other location
If you need any of these, use PickItemAttachment instead."
        )]
        [SerializeField] ItemSpawningData itemReference;

        [SerializeField] bool notCrime;
        [SerializeField, TemplateType(typeof(CrimeOwnerTemplate)), HideIf(nameof(notCrime))] TemplateReference owner;
        
        ARAssetReference _obtainedAssetReference;
        Pickable _pickable;

        protected abstract AvailabilityBase Availability { get; }
        public SpecId Id => SceneId;
        public IInteractableWithHero InteractableWithHero => _pickable;
        public ItemSpawningData ItemData => itemReference;
        public bool IsCrime => !notCrime;
        public CrimeOwnerTemplate CrimeOwner => owner is { IsSet: true } ? owner.Get<CrimeOwnerTemplate>() : null;

        public void Setup(TemplateReference template, int quantity = 1) {
            itemReference = new ItemSpawningData(template, quantity);
        }
        
        void Awake() {
#if UNITY_EDITOR
            if (UnityEditor.EditorApplication.isPlaying)
#endif
            {
                Availability.Init(Spawn, Despawn);
            }
        }
        
        void OnEnable() {
#if UNITY_EDITOR
            EDITOR_RegisterPreview();
            if (UnityEditor.EditorApplication.isPlaying)
#endif
            {
                Availability.Enable();
            }
        }

        void OnDisable() {
#if UNITY_EDITOR
            EDITOR_UnregisterPreview();
            if (UnityEditor.EditorApplication.isPlaying)
#endif
            {
                Availability.Disable();
            }
        }
        
        void OnDestroy() {
#if UNITY_EDITOR
            if (UnityEditor.EditorApplication.isPlaying)
#endif
            {
                Availability.Deinit();
            }
        }

        void Spawn() {
            _pickable = new Pickable(this);
            PickableInitialization.Initialize(_pickable);
        }

        void Despawn() {
            PickableInitialization.Uninitialize(_pickable);
            _pickable = null;
        }

        // === Prefab
        public bool TryGetValidPrefab(out ARAssetReference prefab) {
            try {
                if (_obtainedAssetReference?.IsSet ?? false) {
                    prefab = _obtainedAssetReference;
                    return true;
                }
                if (itemReference.itemTemplateReference is { IsSet: true } reference) {
                    var dropPrefab = reference.Get<ItemTemplate>(this).DropPrefab;
                    _obtainedAssetReference = dropPrefab.Get();
                    prefab = _obtainedAssetReference;
                    if (prefab is { IsSet: true }) {
                        #if UNITY_EDITOR
                        if (string.IsNullOrWhiteSpace(UnityEditor.AssetDatabase.GUIDToAssetPath(prefab.Address))) {
                            LogInvalidPrefab("Drop prefab is set to invalid asset!");
                            prefab = null;
                            return false;
                        }
                        #endif
                        return true;
                    } else {
                        LogInvalidPrefab("Drop prefab is not set!");
                    }
                } else {
                    LogInvalidPrefab("Item is not set!");
                }
            } catch(Exception e) {
                LogInvalidPrefab(e.Message);
                Debug.LogException(e, this);
            }
            prefab = null;
            return false;
        }

        public bool TryLoadPrefab(out ARAsyncOperationHandle<GameObject> handle) {
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

        void LogInvalidPrefab(string message) {
            Log.Important?.Error($"PickableSpec <color=#FFA43B>{gameObject.name}</color> has invalid prefab reference.\n{message}", this);
        }

        void CopyFrom(PickableSpecBase other) {
            itemReference = other.itemReference;
            owner = other.owner;
        }

        // === IAssetReferenceToPreview
        GameObject IWithRenderersToPreview.PreviewParent => gameObject;
        string IWithRenderersToPreview.DisablePreviewKey => "disablePickablePreviews";
        bool IPrefabHandleToPreview.TryLoadPrefabToPreview(out ARAsyncOperationHandle<GameObject> handle) {
            return TryLoadPrefab(out handle);
        }
        void EDITOR_RegisterPreview() => ((IWithRenderersToPreview)this).RegisterToPreview();
        void EDITOR_UnregisterPreview() => ((IWithRenderersToPreview)this).UnregisterFromPreview();
        
        // === Conversion
        [Button, HideInPlayMode]
        void ConvertToPickItemAttachment() {
            var locationSpec = gameObject.AddComponent<LocationSpec>();
            if (TryGetValidPrefab(out var prefab)) {
                locationSpec.prefabReference = prefab;
            }
            locationSpec.snapToGround = false;
            
            gameObject.isStatic = false;
            
            var interactAttachment = gameObject.AddComponent<PickItemAttachment>();
            interactAttachment.SetItemReferenceIfNull(new ItemSpawningData(itemReference.itemTemplateReference, itemReference.quantity));

#if UNITY_EDITOR
            locationSpec.autoConvertToStatic = false;
            locationSpec.ValidatePrefab(true);
            
#endif
            DestroyImmediate(this);
        }

        bool EDITOR_ShowConvertIntoDefaultPickable => this.GetType() != typeof(PickableSpec);

        [Button, HideInPlayMode, ShowIf(nameof(EDITOR_ShowConvertIntoDefaultPickable))]
        void ConvertIntoDefaultPickable() {
            var newSpec = gameObject.AddComponent<PickableSpec>();
            newSpec.CopyFrom(this);
            DestroyImmediate(this);
        }
        
        bool EDITOR_ShowConvertIntoMutablePickable => this.GetType() != typeof(MutablePickableSpec);

        [Button, HideInPlayMode, ShowIf(nameof(EDITOR_ShowConvertIntoMutablePickable))]
        void ConvertIntoMutablePickable() {
            var newSpec = gameObject.AddComponent<MutablePickableSpec>();
            newSpec.CopyFrom(this);
            DestroyImmediate(this);
        }
    }
}