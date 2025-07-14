using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Awaken.ECS.Authoring.LinkedEntities;
using Awaken.ECS.DrakeRenderer.Authoring;
using Awaken.ECS.DrakeRenderer.Utilities;
using Awaken.Kandra;
using Awaken.TG.Assets;
using Awaken.TG.Main.Animations.FSM.Npc.States.General;
using Awaken.TG.Main.Fights.DamageInfo;
using Awaken.TG.Main.Fights.Utils;
using Awaken.Utility.SerializableTypeReference;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using Unity.Entities;
using UnityEngine;
using UnityEngine.ResourceManagement.AsyncOperations;
using MaterialOverrideData = Awaken.ECS.DrakeRenderer.Utilities.MaterialOverrideData;
using Object = UnityEngine.Object;

namespace Awaken.TG.Main.Locations.Attachments.Elements.DeathBehaviours {
    public class GhostDisappearBehaviour : MonoBehaviour, IDeathBehaviour {
        [SerializeField] float delay = 0f;
        [SerializeField] float duration = 1f;
        [SerializeField] GameObject[] objectsToDisable = Array.Empty<GameObject>();
        [SerializeField] string disappearShaderPropertyID = "_Ghost_Transparency";
        [SerializeField, MaterialPropertyComponent] SerializableTypeReference disappearSerializedType;
        [SerializeField] bool invertTransition = true;
        [SerializeField] bool useDeathAnimation;
        [ARAssetReferenceSettings(new [] {typeof(GameObject)}, true, AddressableGroup.NPCs), SerializeField]
        ARAssetReference ashPrefabRef;

        Renderer[] _renderers;
        KandraRenderer[] _kandraRenderers;
        LinkedEntitiesAccess[] _linkedEntities;
        MaterialOverrideData _materialOverride;

        List<MaterialWrapper> _ghostMaterialHandlers;
        CancellationTokenSource _delayCancellationTokenSource;

        ARAsyncOperationHandle<GameObject> _ashHandle;
        
        public bool UseDeathAnimation => useDeathAnimation;
        public NpcDeath.DeathAnimType UseCustomDeathAnimation => NpcDeath.DeathAnimType.Default;
        bool HasAshPrefab => ashPrefabRef is {IsSet: true};
        bool StillExists => this != null;

        public void OnVisualLoaded(DeathElement death, Transform transform) { }

        public void OnDeath(DamageOutcome damageOutcome, Location location) {
            int transparencyID = Shader.PropertyToID(disappearShaderPropertyID);
            
            InitGeneric(transparencyID);
            InitKandra(transparencyID);
            InitDrake(transparencyID);

            if (HasAshPrefab) {
                ashPrefabRef.Preload<GameObject>(() => StillExists).Forget();
            }
            
            if (delay > 0) {
                DelayDisappear(transparencyID).Forget();
            } else {
                Disappear(transparencyID);
            }
        }
        
        async UniTaskVoid DelayDisappear(int transparencyID) {
            if (delay > 0) {
                _delayCancellationTokenSource = new CancellationTokenSource();
                if (!await AsyncUtil.DelayTime(this, delay, source: _delayCancellationTokenSource)) {
                    return;
                }
            }

            Disappear(transparencyID);
        }

        void Disappear(int transparencyID) {
            float progress = 0f;
            Tween tween = DOTween.To(() => progress, SetProgress, 1f, duration).SetEase(Ease.InQuad);
            tween.OnComplete(FinishDeathAnimation);
            tween.OnKill(FinishDeathAnimation);
            
            void SetProgress(float v) {
                if (!StillExists) return;
                progress = v;
                float value = invertTransition ? 1f - progress : progress;
                UpdateGenericAndKandra(transparencyID, value);
                UpdateDrake(transparencyID, value);
            }
        }

        void FinishDeathAnimation() {
            if (!_ghostMaterialHandlers.Any()) return;

            Cleanup();

            if (HasAshPrefab && StillExists) {
                SpawnAshPrefab();
            }

            if (objectsToDisable != null) {
                foreach (var obj in objectsToDisable.Where(o => o != null)) {
                    obj.SetActive(false);
                }
            }
        }
        
        void SpawnAshPrefab() {
            _ashHandle = ashPrefabRef.LoadAsset<GameObject>();
            _ashHandle.OnComplete(h => {
                if (gameObject == null || h.Status != AsyncOperationStatus.Succeeded || h.Result == null) {
                    ReleaseAshHandle();
                    return;
                }
               
                var ashInstance = Object.Instantiate(h.Result, transform);
                ashInstance.transform.position = transform.position;
            });
        }

        void Cleanup() {
            if (_renderers == null || _kandraRenderers == null) {
                return;
            }
            
            CleanupGenericAndKandra();
            CleanupDrake();
        }

        void OnDestroy() {
            Cleanup();
            ReleaseAshHandle();
            _delayCancellationTokenSource?.Cancel();
            _delayCancellationTokenSource = null;
        }
        
        // Generic
        void InitGeneric(int transparencyID) {
            _renderers = transform.GetComponentsInChildren<Renderer>();
            _ghostMaterialHandlers = _renderers.SelectMany(r => r.materials)
                .Where(m => m.HasProperty(transparencyID))
                .Select(m => new MaterialWrapper(m, true))
                .ToList();
        }

        void CleanupGeneric() {
            foreach (var cachedRenderer in _renderers) {
                if (cachedRenderer == null) {
                    continue;
                }
                cachedRenderer.enabled = false;
            }
            _renderers = null;
        }

        // Kandra
        void InitKandra(int transparencyID) {
            _kandraRenderers = transform.GetComponentsInChildren<KandraRenderer>();
            foreach (var kandra in _kandraRenderers) {
                var tempMaterials = kandra.UseInstancedMaterials();
                _ghostMaterialHandlers.AddRange(tempMaterials.Where(m => m.HasProperty(transparencyID))
                    .Select(m => new MaterialWrapper(m, false)));
            }
        }

        void CleanupKandra() {
            foreach (var kandraRenderer in _kandraRenderers) {
                if (kandraRenderer == null) {
                    continue;
                }
                kandraRenderer.enabled = false;
                kandraRenderer.UseOriginalMaterials();
            }
            _kandraRenderers = null;
        }
                
        // Generic + Kandra
        void UpdateGenericAndKandra(int transparencyID, float value) {
            foreach (var materialHandler in _ghostMaterialHandlers) {
                materialHandler.material.SetFloat(transparencyID, value);
            }
        }

        void CleanupGenericAndKandra() {
            CleanupGeneric();
            CleanupKandra();
            foreach (var materialHandler in _ghostMaterialHandlers) {
                if (materialHandler.manuallyHandled) {
                    Object.Destroy(materialHandler.material);
                }
            }
            _ghostMaterialHandlers.Clear();
        }

        // Drake
        void InitDrake(int transparencyID) {
            var startingValue = invertTransition ? 1f : 0f;
            _materialOverride = new MaterialOverrideData(TypeManager.GetTypeIndex(disappearSerializedType), startingValue);
            _linkedEntities = GetComponentsInChildren<LinkedEntitiesAccess>();
            foreach (var linkedEntitiesAccess in _linkedEntities) {
                MaterialOverrideUtils.ApplyMaterialOverrides(linkedEntitiesAccess, _materialOverride);
            }
        }

        void UpdateDrake(int transparencyID, float value) {
            _materialOverride.SetValue(value);
            foreach (var linkedEntitiesAccess in _linkedEntities) {
                MaterialOverrideUtils.ApplyMaterialOverrides(linkedEntitiesAccess, _materialOverride);
            }
        }

        void CleanupDrake() {
            _linkedEntities = null;
        }

        void ReleaseAshHandle() {
            if (_ashHandle.IsValid()) {
                _ashHandle.Release();
                _ashHandle = default;
            }
        }
    }

    internal readonly struct MaterialWrapper {
        public readonly Material material;
        public readonly bool manuallyHandled;

        public MaterialWrapper(Material material, bool manuallyHandled) {
            this.material = material;
            this.manuallyHandled = manuallyHandled;
        }
    }
}