using System;
using System.Collections.Generic;
using System.Linq;
using Awaken.Kandra;
using Awaken.TG.Assets;
using Awaken.TG.Main.Fights.Utils;
using Awaken.TG.Main.Heroes.SkinnedBones;
using Awaken.TG.Main.Utility.Debugging;
using Awaken.TG.MVC;
using Awaken.TG.MVC.Elements;
using Awaken.TG.MVC.Events;
using Awaken.Utility.Debugging;
using Awaken.Utility.GameObjects;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.ResourceManagement.AsyncOperations;
using LogType = Awaken.Utility.Debugging.LogType;

namespace Awaken.TG.Main.Locations.Mobs {
    public interface IBaseClothes : IElement {
        void Equip(ARAssetReference reference, BaseClothes.ShadowsOverride shadowsOverride = BaseClothes.ShadowsOverride.None, bool withEvent = true);
        UniTask<(GameObject, bool)> EquipTask(ARAssetReference reference, BaseClothes.ShadowsOverride shadowsOverride = BaseClothes.ShadowsOverride.None, bool withEvent = true);
        
        void Unequip(ARAssetReference reference);
        
        IEnumerable<GameObject> LoadedClothes { get; }
    }
    
    public interface IBaseClothes<out T> : IBaseClothes, IElement<T> where T : IModel { }
    
    public abstract partial class BaseClothes : Element, IBaseClothes {
        protected abstract Transform ParentTransform { get; }
        protected GameObject _baseMesh;

        protected KandraRig _kandraRig;

        protected Dictionary<ARAssetReference, SpawnedCloth> _equipped = new();

        protected virtual uint? LightRenderLayerMask => null;
        protected virtual bool IsKandraHidden => false;
        
        public new class Events {
            public static readonly Event<IBaseClothes, GameObject> ClothEquipped = new (nameof(ClothEquipped));
            public static readonly Event<IBaseClothes, GameObject> ClothBeingUnequipped = new (nameof(ClothBeingUnequipped));
        }

        protected sealed override void OnInitialize() {
            var animator = FindAnimator();
            if (animator != null) {
                _baseMesh = animator.gameObject;
                _kandraRig = _baseMesh.GetComponent<KandraRig>();
            } else {
                Log.Important?.Error($"Failed to find animator in model: {GenericParentModel}", ParentTransform);
            }
        }

        public IEnumerable<GameObject> LoadedClothes => _equipped.Values.Select(cloth => cloth.Instance).Where(go => go != null);
        
        public async UniTask<(GameObject, bool)> EquipTask(ARAssetReference reference,
            ShadowsOverride shadowsOverride = BaseClothes.ShadowsOverride.None, bool withEvent = true) {
            if (!reference.IsSet) return (null, false);
            (SpawnedCloth cloth, UniTask<bool> task) = EquipCloth(reference, shadowsOverride, withEvent);
            if (cloth == null) {
                return (null, false);
            }
            bool result = await task;
            return (cloth.Instance, result);
        }

        public void Equip(ARAssetReference reference, ShadowsOverride shadowsOverride = ShadowsOverride.None, bool withEvent = true) {
            EquipCloth(reference, shadowsOverride, withEvent);
        }

        (SpawnedCloth, UniTask<bool>) EquipCloth(ARAssetReference reference, ShadowsOverride shadowsOverride = ShadowsOverride.None, bool withEvent = true) {
            if (reference == null || !reference.IsSet) {
                Log.Important?.Error($"Trying to equip empty cloth for {GenericParentModel}");
                return (null, UniTask.FromResult(false));
            }

            var cloth = new SpawnedCloth();

            if (!_equipped.TryAdd(reference, cloth)) {
                Log.Minor?.Warning($"Trying to equip already equipped cloth for {GenericParentModel} - {reference.RuntimeKey}", ParentTransform);
                return (null, UniTask.FromResult(false));
            }
            
            var prefabHandle = reference.LoadAsset<GameObject>();
            cloth.AssignPrefabReference(reference);

            var task = EquipAfterLoaded(prefabHandle, cloth, shadowsOverride, withEvent);

            return (cloth, task);
        }
        
        public void Unequip(ARAssetReference reference) {
            if (_equipped.Remove(reference, out var cloth)) {
                DiscardCloth(cloth);
            }
        }

        public void SafeUnequip(ARAssetReference reference) {
            if (_equipped.Remove(reference, out var cloth)) {
                if (cloth.Instance is {activeInHierarchy: true}) {
                    var magicaClothes = cloth.Instance.GetComponentsInChildren<MagicaCloth2.MagicaCloth>();
                    if (magicaClothes.Length > 0) {
                        DelayedDiscardedCloth(cloth).Forget();
                        return;
                    }
                }

                DiscardCloth(cloth);
            }
        }

        protected virtual Animator FindAnimator() {
            return ParentTransform.GetComponentInChildren<Animator>(true);
        }

        async UniTaskVoid DelayedDiscardedCloth(SpawnedCloth cloth) {
            cloth.Instance.SetActive(false);
            await AsyncUtil.DelayFrame(this);
            DiscardCloth(cloth);
        }

        void DiscardCloth(SpawnedCloth cloth) {
            if (cloth.Instance != null) {
                if (!this.HasBeenDiscarded) {
                    this.Trigger(Events.ClothBeingUnequipped, cloth.Instance);
                }
                GameObjects.DestroySafely(cloth.Instance);
            }
            cloth.Prefab.ReleaseAsset();
            cloth.Clear();
        }

        async UniTask<bool> EquipAfterLoaded(ARAsyncOperationHandle<GameObject> clothHandle, SpawnedCloth cloth, ShadowsOverride shadowsOverride, bool withEvent) {
            var clothPrefab = await clothHandle.ToUniTask();
            if (clothHandle.IsCancelled) {
                return false;
            }
            
            // We need to spawn Kandra before LateUpdate and as we don't know what exact loop point we are, then just wait always for first valid point
            await UniTask.DelayFrame(1, PlayerLoopTiming.EarlyUpdate);
            
            if (clothHandle.IsCancelled) {
                return false;
            }
            
            if (clothHandle.Status != AsyncOperationStatus.Succeeded) {
                Log.Minor?.Error($"Can not load cloth ' {cloth.Prefab.Address} ' for model: {LogUtils.GetDebugName(GenericParentModel)}");
                return false;
            }

            var stitched = ClothStitcher.Stitch(clothPrefab, _kandraRig);
            cloth.AssignInstance(stitched);

            var kandraRenderers = cloth.Instance.GetComponentsInChildren<KandraRenderer>(true);
            foreach (var kandraRenderer in kandraRenderers) {
                var existingData = kandraRenderer.rendererData;
                var renderingData = new KandraRenderer.RendererFilteringSettings() {
                    renderingLayersMask = LightRenderLayerMask ?? existingData.filteringSettings.renderingLayersMask,
                    shadowCastingMode = shadowsOverride switch {
                        ShadowsOverride.None => existingData.filteringSettings.shadowCastingMode,
                        ShadowsOverride.ForceOff => ShadowCastingMode.Off,
                        _ => throw new ArgumentOutOfRangeException(nameof(shadowsOverride), shadowsOverride, null)
                    },
                };
                if (renderingData.Equals(existingData.filteringSettings) == false) {
                    kandraRenderer.SetFilteringSettings(renderingData);
                }

                if (IsKandraHidden) {
                    kandraRenderer.enabled = false;
                }
            }

            if (withEvent) {
                this.Trigger(Events.ClothEquipped, cloth.Instance);
            }
            return true;
        }

        protected override void OnDiscard(bool fromDomainDrop) {
            if (_equipped != null) {
                foreach (var cloth in _equipped.Values) {
                    DiscardCloth(cloth);
                }
                _equipped = null;
            }
        }

        protected class SpawnedCloth {
            public ARAssetReference Prefab { get; private set; }
            public GameObject Instance { get; private set; }

            public void AssignPrefabReference(ARAssetReference prefab) {
                Prefab = prefab;
            }

            public void AssignInstance(GameObject instance) {
                Instance = instance;
            }

            public void Clear() {
                Prefab = default;
                Instance = null;
            }
        }

        public enum ShadowsOverride : byte {
            None,
            ForceOff,
        }
    }
    
    public abstract partial class BaseClothes<T> : BaseClothes, IBaseClothes<T> where T : IModel {
        public T ParentModel => (T) GenericParentModel;
    }
}