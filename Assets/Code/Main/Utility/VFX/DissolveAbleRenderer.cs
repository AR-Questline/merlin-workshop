using System;
using System.Linq;
using Awaken.ECS.Authoring.LinkedEntities;
using Awaken.ECS.DrakeRenderer.Authoring;
using Awaken.Kandra;
using Awaken.TG.Assets;
using Awaken.TG.Main.Locations;
using Awaken.TG.Main.Utility.Debugging;
using Awaken.TG.Main.VisualGraphUtils;
using Awaken.Utility.Debugging;
using Awaken.Utility.GameObjects;
using Awaken.Utility.SerializableTypeReference;
using Sirenix.OdinInspector;
using UnityEngine;
using LogType = Awaken.Utility.Debugging.LogType;

namespace Awaken.TG.Main.Utility.VFX {
    /// <summary>
    /// Marker script for renderers that are available to be dissolved.
    /// </summary>
    public class DissolveAbleRenderer : MonoBehaviour, IDissolveAble {
        [SerializeField, BoxGroup("Type Setup")] bool isWeapon;
        [SerializeField, BoxGroup("Type Setup")] bool isCloth = true;
        [SerializeField] public bool dontReplaceMaterials;
        [SerializeField, Tooltip("Materials linked below will be used, original textures won't get copied to them.")] 
        public bool hasCustomGhostMaterials;
        [SerializeField, HideIf(nameof(dontReplaceMaterials)), ReadOnly] public Material[] dissolveAbleMaterials = Array.Empty<Material>();
        [SerializeField, HideIf(nameof(dontReplaceMaterials)), ARAssetReferenceSettings(new[] {typeof(Material)}, group: AddressableGroup.DrakeRenderer)] public ARAssetReference[] dissolveAbleMaterialRefs = Array.Empty<ARAssetReference>();
        
        IDissolveAbleRendererWrapper _wrapper;
        IDissolveAbleDissolveController _dissolveController;
        
        public bool IsInDissolvableState => _wrapper?.InDissolvableState ?? false;
        public bool IsWeapon => isWeapon;
        public bool IsCloth => isCloth;
        [UnityEngine.Scripting.Preserve] public Material[] InstancedMaterials => _wrapper.InstancedMaterialsForExternalModifications;

        void Awake() {
            Init(true);
        }

        public void Init(bool fromStart = false) {
            if (_wrapper != null) {
                return;
            }
            if (GetComponent<KandraRenderer>() is {} kandra) {
                _wrapper = new DissolveAbleRendererKandraWrapper(this, kandra);
            } else if (TryGetDrakeLinkedEntitiesAccess(out var linkedEntitiesAccess)) {
                if (linkedEntitiesAccess == null) {
                    _wrapper = new DissolveAbleRendererUnpreparedDrakeWrapper(this);
                } else {
                    _wrapper = new DissolveAbleRendererDrakeWrapper(this, linkedEntitiesAccess);
                }
            } else {
                Log.Minor?.Error($"There is a Unity Renderer on {gameObject} that should be a KandraRenderer or DrakeMeshRenderer", this);
                return;
            }
            
            _wrapper.Init();
            
#if UNITY_EDITOR
            for (int i = 0; i < dissolveAbleMaterials.Length; i++) {
                if (dissolveAbleMaterials[i] == null) {
                    LogError($"DissolveAbleRenderer has null material at index {i}", LogType.Critical);
                }
            }
#endif
        }
        
        public void ForceWrapperChange(IDissolveAbleRendererWrapper wrapper) {
            _wrapper?.Destroy();
            _wrapper = wrapper;
            _wrapper.Init();
        }

        bool TryGetDrakeLinkedEntitiesAccess(out LinkedEntitiesAccess linkedEntitiesAccess) {
            if (GetComponent<LinkedEntitiesAccess>() is { } foundLinkedEntitiesAccess) {
                linkedEntitiesAccess = foundLinkedEntitiesAccess;
                return true;
            } 
            if (GetComponent<DrakeLodGroup>() is not null || GetComponent<DrakeMeshRenderer>() is not null) { 
                linkedEntitiesAccess = null;
                return true;
            }
            linkedEntitiesAccess = null; 
            return false;
        }

        void OnDestroy() {
            if (_dissolveController != null) {
                _dissolveController.RemoveRenderer(this);
                _dissolveController = null;
            }

            _wrapper?.Destroy();
            _wrapper = null;
            
            dissolveAbleMaterials = Array.Empty<Material>();
        }

        public void AssignController(IDissolveAbleDissolveController controller) {
            _dissolveController = controller;
        }

        public void CopyFrom(DissolveAbleRenderer other) {
            dontReplaceMaterials = other.dontReplaceMaterials;
            hasCustomGhostMaterials = other.hasCustomGhostMaterials;
            if (!dontReplaceMaterials) {
                dissolveAbleMaterials = other.dissolveAbleMaterials.ToArray();
                dissolveAbleMaterialRefs = other.dissolveAbleMaterialRefs.ToArray();
            }
            Init();
        }

        public void SetCustomDissolveAbleMaterials(Material[] materials, ARAssetReference[] materialRefs, bool forceReplaceOnNonRepreceableDAR = false) {
            if (IsInDissolvableState) {
                LogError("DissolveAbleRenderer can not change to DissolveAble because already is", LogType.Critical);
                return;
            }
            
            if (dontReplaceMaterials) {
                if (forceReplaceOnNonRepreceableDAR) {
                    dontReplaceMaterials = false;
                } else {
                    LogError("Trying to set custom materials on a DissolveAbleRenderer that is not set to have replaceable materials", LogType.Minor);
                    return;
                }
            }

            dissolveAbleMaterials = materials;
            dissolveAbleMaterialRefs = materialRefs;
        }

        public void ChangeToDissolveAble() {
            if (_wrapper == null) {
#if UNITY_EDITOR
                LogError("Null renderer for dissolvable renderer", LogType.Critical);
#endif
                return;
            }

            if (IsInDissolvableState) {
                return;
            }
            
            _wrapper.ChangeToDissolveAble();
        }

        public void RestoreToOriginal() {
            if (_wrapper == null) {
#if UNITY_EDITOR
                LogError($"Null renderer for dissolvable renderer", LogType.Critical);
#endif
                return;
            }

            if (!IsInDissolvableState) {
                LogError("DissolveAbleRenderer can not change to original because already is", LogType.Critical);
                return;
            }

            _wrapper.RestoreToOriginalMaterials();
        }

        public void InitPropertyModification(SerializableTypeReference serializedType, float value) {
            if (_wrapper == null) {
#if UNITY_EDITOR
                LogError($"Null renderer for dissolvable renderer", LogType.Critical);
#endif
                return;
            }
            
            _wrapper.InitPropertyModification(serializedType, value);
        }

        public void UpdateProperty(SerializableTypeReference serializedType, float value) {
            if (_wrapper == null) {
#if UNITY_EDITOR
                LogError($"Null renderer for dissolvable renderer", LogType.Critical);
#endif
                return;
            }
            
            _wrapper.UpdateProperty(serializedType, value);
        }

        public void FinishPropertyModification(SerializableTypeReference serializedType) {
            if (_wrapper == null) {
#if UNITY_EDITOR
                LogError($"Null renderer for dissolvable renderer", LogType.Critical);
#endif
                return;
            }
            
            _wrapper.FinishPropertyModification(serializedType);
        }

        public void LogError(string error, LogType type) {
            var location = VGUtils.GetModel<Location>(transform.gameObject);
            string locationInfo = location != null ? LogUtils.GetDebugName(location) : null;
            string path = gameObject.PathInSceneHierarchy();
#pragma warning disable CS0618 // Type or member is obsolete
            Log.When(type)?.Error($"{error} : {transform.name}, {locationInfo}\n{path}", this);
#pragma warning restore CS0618 // Type or member is obsolete
        }
    }
}