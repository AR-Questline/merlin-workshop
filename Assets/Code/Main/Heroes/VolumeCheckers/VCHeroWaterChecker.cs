using System.Collections.Generic;
using Awaken.TG.Main.AudioSystem;
using Awaken.TG.Main.AudioSystem.Biomes;
using Awaken.TG.Main.Heroes.Interactions;
using Awaken.TG.Main.Scenes.SceneConstructors;
using Awaken.TG.Main.Settings.Controllers;
using Awaken.TG.Main.Timing.ARTime;
using Awaken.TG.Main.UI.TitleScreen.Loading;
using Awaken.TG.Main.Utility.Water;
using Awaken.TG.MVC;
using Awaken.TG.MVC.Domains;
using Awaken.TG.MVC.Events;
using Awaken.Utility;
using Awaken.Utility.Extensions;
using DG.Tweening;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.HighDefinition;

namespace Awaken.TG.Main.Heroes.VolumeCheckers {
    [RequireComponent(typeof(Collider))]
    public sealed class VCHeroWaterChecker : VCHeroVolumeChecker, IRainIntensityModifier {
        const float MinimumDepthForFeet = 0.02f;
        const float MinWaterCheckDistance = 4f;
        const float MaxWaterCheckDistance = 20f;
        
        [FoldoutGroup("Audio"), SerializeField, Required] ARFmodEventEmitter fmodEmitter;
        [FoldoutGroup("Audio"), SerializeField, Required] ARFmodEventEmitter fmodSnapshotEmitter;
        [FoldoutGroup("Volume"), SerializeField, Required] Volume defaultWaterVolume;
        [FoldoutGroup("Volume"), SerializeField, Required] Volume waterVolumeOverride;
        [FoldoutGroup("Volume"), SerializeField] float volumeTweenDuration = 2f;
        [FoldoutGroup("Distances"), SerializeField] float underWaterDepth;
        [FoldoutGroup("Sampling"), SerializeField] WaterSurfaceSampler.Settings waterSampleSettings;

        bool _inWater;
        bool _usingOverrideVolume;
        bool _feetInWater;
        WaterSurface _inWaterCollider;
        bool _waterRayHasHit;
        Tweener _underwaterTween;
        SphereCollider _sphereCollider;
        SceneService _sceneService;
        WaterSurfaceSampler _waterSurfaceSampler;
        List<IVolumeController> _overrideVolumeControllers = new(3);
        
        AudioCore AudioCore => Services.Get<AudioCore>();
        public Component Owner => this;
        public float MultiplierWhenUnderRoof => Services.Get<CommonReferences>().AudioConfig.rainIntensityMultiplierWhenUnderWater;

        public static class Events {
            public static readonly Event<Hero, bool> WaterCollisionStateChanged = new(nameof(WaterCollisionStateChanged));
            public static readonly Event<Hero, HeroInWaterData> WaterCollisionUpdate = new(nameof(WaterCollisionUpdate));
            public static readonly Event<Hero, bool> FeetWaterRaycastChanged = new(nameof(FeetWaterRaycastChanged));
        }

        protected override void OnAttach() {
            base.OnAttach();
            _sceneService = World.Services.Get<SceneService>();
            _sphereCollider = GetComponent<SphereCollider>();
            
            _waterSurfaceSampler = new WaterSurfaceSampler(waterSampleSettings);
            Target.GetOrCreateTimeDependent().WithUpdate(RaycastWaterChecker);
            
            waterVolumeOverride.GetComponents(_overrideVolumeControllers);
        }

        protected override void OnFirstVolumeEnter(Collider other) {
            _inWaterCollider = other.GetComponent<WaterSurface>();
        }

        protected override void OnAllVolumesExit() {
            _inWaterCollider = null;
        }

        protected override void OnDiscard() {
            _underwaterTween.Kill();
            Target.GetTimeDependent()?.WithoutUpdate(RaycastWaterChecker);
            StopSound();
            _waterSurfaceSampler.Dispose();
        }

        void EnterWater(WaterSurface foundSurface) {
            _inWater = true;
            WaterSurface.currentWater = foundSurface;
            
            HandleVolume(foundSurface);

            Target.Trigger(Events.WaterCollisionStateChanged, true);
            AudioCore.SetRainIntensityMultiplier(this);
            if (!World.HasAny<LoadingScreenUI>()) {
                // fmodEmitter.Play();
                // fmodSnapshotEmitter.Play();
            }
        }

        void ExitWater() {
            _inWater = false;
            WaterSurface.currentWater = null;
            WaterTween(false);
            Target.Trigger(Events.WaterCollisionStateChanged, false);
            StopSound();
        }
        
        void HandleVolume(WaterSurface foundSurface) {
            VolumeProfile volumeOverride = foundSurface.GetComponent<WaterVolumeOverride>()?.profile;
            if (volumeOverride) {
                _usingOverrideVolume = true;
                waterVolumeOverride.sharedProfile = volumeOverride;
                defaultWaterVolume.gameObject.SetActive(false);
                WaterTween(true);
                
                foreach (var controller in _overrideVolumeControllers) {
                    controller.NewVolumeProfileLoaded();
                }
            } else {
                _usingOverrideVolume = false;
                waterVolumeOverride.gameObject.SetActive(false);
                WaterTween(true);
            }
        }

        void WaterTween(bool enteringWater) {
            var volumeToTween = _usingOverrideVolume ? waterVolumeOverride : defaultWaterVolume;
            if (enteringWater) {
                volumeToTween.gameObject.SetActive(true);
            }
            _underwaterTween.KillWithoutCallback();
            _underwaterTween = DOTween.To(() => volumeToTween.weight, x => volumeToTween.weight = x, enteringWater? 1 : 0, volumeTweenDuration)
                .OnKill(() => ResetTween(enteringWater));
            if (enteringWater == false)
                _underwaterTween.OnComplete(() => volumeToTween.gameObject.SetActive(false));
        }

        void ResetTween(bool playerIsUnderwater) {
            if (_usingOverrideVolume) {
                waterVolumeOverride.weight = playerIsUnderwater? 1 : 0;
                waterVolumeOverride.gameObject.SetActive(playerIsUnderwater);
            } else {
                defaultWaterVolume.weight = playerIsUnderwater? 1 : 0;
                defaultWaterVolume.gameObject.SetActive(playerIsUnderwater);
            }
            _underwaterTween = null;
        }

        void StopSound() {
            AudioCore.RestoreRainIntensityMultiplier(this);
            // fmodEmitter.Stop();
            // fmodSnapshotEmitter.Stop();
        }

        protected override void OnStay() { }
        
        void RaycastWaterChecker(float deltaTime) {
            Vector3 raycastStart = transform.position;
            float minimumDepth = _sphereCollider.center.y - _sphereCollider.radius;

            bool inWater = _inWaterCollider;
            bool feetInWater = false;
            
            bool useLongerRaycast = !_inWaterCollider && _sceneService.IsOpenWorld;
            float raycastDistance = useLongerRaycast ? MaxWaterCheckDistance : MinWaterCheckDistance;
            
            bool lastRayHasHit = _waterRayHasHit;
            //We need to cast from above because water is a plane
            _waterRayHasHit = Physics.Raycast(raycastStart + Vector3.up * raycastDistance, Vector3.down, out RaycastHit hit, raycastDistance, RenderLayers.Mask.Water);
            if (_waterRayHasHit) {
                float waterOffset = CalculateWaterSurfaceOffset(hit.collider.gameObject, hit.point, deltaTime);
                float distanceToWaterSurface = raycastDistance - hit.distance + waterOffset;
                inWater = distanceToWaterSurface > minimumDepth;

                if (distanceToWaterSurface > MinimumDepthForFeet) {
                    feetInWater = true;
                }

                Target.Trigger(Events.WaterCollisionUpdate, new HeroInWaterData {
                    isUnderwater = inWater,
                    hasRaycastHit = true,
                    distanceToWaterSurface = distanceToWaterSurface,
                    waterSurfaceOffset = waterOffset,
                });
            } else if (lastRayHasHit) {
                Target.Trigger(Events.WaterCollisionUpdate, new HeroInWaterData {
                    isUnderwater = inWater,
                    hasRaycastHit = false,
                    distanceToWaterSurface = 0.0f,
                    waterSurfaceOffset = 0.0f,
                });
            }

            if (inWater) {
                if (!_inWater) {
                    WaterSurface foundSurface = hit.transform != null 
                        ? hit.transform.GetComponent<WaterSurface>() 
                        : _inWaterCollider;
                    
                    EnterWater(foundSurface);
                }
            } else {
                if (_inWater) {
                    ExitWater();
                }
            }

            if (feetInWater) {
                if (!_feetInWater) {
                    _feetInWater = true; 
                    Target.Trigger(Events.FeetWaterRaycastChanged, true);
                }
            } else {
                if (_feetInWater) {
                    _feetInWater = false;
                    Target.Trigger(Events.FeetWaterRaycastChanged, false);
                }
            }
        }

        float CalculateWaterSurfaceOffset(GameObject waterGameObject, Vector3 checkPoint, float deltaTime) {
            var waterSurface = waterGameObject.GetComponent<WaterSurface>();
            if (waterSurface == null) {
                return 0.0f;
            }
            _waterSurfaceSampler.RequestSample(waterSurface, checkPoint);
            _waterSurfaceSampler.ProgressEasing(deltaTime);
            return _waterSurfaceSampler.EasedOffset.y;
        }

        public struct HeroInWaterData {
            [UnityEngine.Scripting.Preserve]  public bool isUnderwater;
            public bool hasRaycastHit;
            public float distanceToWaterSurface;
            public float waterSurfaceOffset;
        }
    }
}
