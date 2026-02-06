using Awaken.CommonInterfaces;
using Awaken.ECS.DrakeRenderer.Authoring;
using Awaken.TG.Assets;
using Awaken.TG.Graphics.VFX;
using Awaken.TG.Main.Grounds.CullingGroupSystem;
using Awaken.TG.Main.Grounds.CullingGroupSystem.CullingGroups;
using Awaken.TG.Main.Locations;
using Awaken.TG.Main.Locations.Actions;
using Awaken.TG.MVC;
using Awaken.TG.MVC.Elements;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using UnityEngine;
using UnityEngine.ResourceManagement.AsyncOperations;

namespace Awaken.TG.Main.Fights.NPCs {
    public partial class DeadBodyMarkerVFX : Element<Location>, UnityUpdateProvider.IWithUpdateGeneric {
        public override bool IsNotSaved => true;

        bool _visible;
        bool _registeredToUpdate;
        
        ShareableARAssetReference _vfxRef;
        IPooledInstance _vfxInstance;
        Transform _vfxTransform;
        readonly Transform _transformToFollow;
        
        public DeadBodyMarkerVFX(ShareableARAssetReference vfxRef, Transform transformToFollow = null) {
            _vfxRef = vfxRef;
            _transformToFollow = transformToFollow;
        }

        protected override void OnInitialize() {
            ParentModel.OnVisualLoaded(t => OnVisualLoaded(t).Forget());
            return;

            async UniTaskVoid OnVisualLoaded(Transform transform) {
                _vfxInstance = await PrefabPool.Instantiate(_vfxRef, Vector3.zero, Quaternion.identity, transform);
                if (HasBeenDiscarded || _vfxInstance == null) {
                    _vfxInstance?.Release();
                    _vfxInstance = null;
                    return;
                }
                
                _vfxTransform = _vfxInstance.Instance.transform;
                _vfxTransform.rotation = Quaternion.identity;
                
                IWithUnityRepresentation.Options options = new() { movable = true, linkedLifetime = true };
                if (_vfxTransform.TryGetComponent(out DrakeLodGroup prefabInstanceDrakeLodGroup)) {
                    prefabInstanceDrakeLodGroup.SetUnityRepresentation(options);
                    foreach (var drakeMeshRenderer in prefabInstanceDrakeLodGroup.Renderers) {
                        if (drakeMeshRenderer == null) {
                            continue;
                        }
                        drakeMeshRenderer.SetUnityRepresentation(options);
                    }
                }

                ParentModel.ListenTo(Location.Events.ItemPickedFromLocation, Discard, this);
                ParentModel.ListenTo(ICullingSystemRegistreeModel.Events.DistanceBandChanged, OnBandChanged, this);
                OnBandChanged(ParentModel.GetCurrentBandSafe(0));
                CheckIfEmpty();
            }
        }

        void CheckIfEmpty() {
            if (!ParentModel.TryGetElement(out SearchAction search) || search.IsEmpty()) {
                Discard();
            }
        }

        void OnBandChanged(int band) {
            _visible = LocationCullingGroup.InNpcVisibilityBand(band);
            _vfxTransform.gameObject.SetActive(_visible);

            if (_visible && _transformToFollow != null) {
                RegisterToUpdate();
            } else {
                UnregisterFromUpdate();
            }
        }

        public void UnityUpdate() {
            if (_transformToFollow == null) {
                UnregisterFromUpdate();
                _vfxTransform.position = Vector3.zero;
                return;
            }
                
            _vfxTransform.position = _transformToFollow.position;
        }

        void RegisterToUpdate() {
            if (!_registeredToUpdate) {
                UnityUpdateProvider.GetOrCreate().RegisterGeneric(this);
                _registeredToUpdate = true;
            }
        }
        
        void UnregisterFromUpdate() {
            UnityUpdateProvider.TryGet()?.UnregisterGeneric(this);
            _registeredToUpdate = false;
        }
        

        protected override void OnDiscard(bool fromDomainDrop) {
            if (!fromDomainDrop) {
                ParentModel.TryGetElement<NpcDummy>()?.MarkDeadBodyMarkerDiscarded();
            }
            
            UnityUpdateProvider.TryGet()?.UnregisterGeneric(this);
            
            if (_vfxInstance != null) {
                VFXUtils.StopVfxAndReturn(_vfxInstance, 5f);
                foreach (var animatedProperties in _vfxInstance.Instance.GetComponentsInChildren<DrakeAnimatedPropertiesOverrideController>()) {
                    animatedProperties.StartBackward();
                }
                _vfxInstance = null;
            }
        }
    }
}