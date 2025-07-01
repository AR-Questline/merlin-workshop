using Awaken.TG.Assets;
using Awaken.TG.Code.Utility;
using Awaken.TG.Main.AudioSystem;
using Awaken.TG.Main.Fights;
using Awaken.TG.Main.Fights.Utils;
using Awaken.TG.Main.General;
using Awaken.TG.Main.General.Configs;
using Awaken.TG.Main.Grounds;
using Awaken.TG.Main.Heroes.Combat;
using Awaken.TG.Main.Locations;
using Awaken.TG.Main.Locations.Setup;
using Awaken.TG.Main.Saving.Models;
using Awaken.TG.Main.Scenes.SceneConstructors;
using Awaken.TG.MVC.Utils;
using Awaken.TG.Utility;
using Awaken.Utility.Debugging;
using Cysharp.Threading.Tasks;
using Pathfinding;
using UnityEngine;

namespace Awaken.TG.Main.Heroes.WyrdStalker {
    internal abstract class WyrdStalkerControllerBase {
        const float OnScreenOuterDotThreshold = 0.5f;
        const float AcceptableAngleThreshold = 10f;
        const int MaxAngleTries = 5;
        const int MaxRangeTries = 3;

        protected const float WyrdStalkerHeight = 2f;
        
        public const float UnspawnedCheckInterval = 15f;
        const float SpawnedCheckInterval = 0.25f;
        
        static float AfterHiddenNextShowCooldown => GameConstants.Get.wyrdStalkerAfterHiddenNextShowCooldown;
        static FloatRange WyrdStalkerSpawnRange => GameConstants.Get.wyrdStalkerSpawnRange;
        static float VisibilityCoyoteTime => GameConstants.Get.visibilityCoyoteTime;
        static float VisibilityCoyoteTimeDecreaseSpeed => GameConstants.Get.visibilityCoyoteTimeDecreaseSpeed;

        float _nextCheckTime;
        float _visibilityCoyoteTime;
        
        Camera _camera;
        float _cameraFoVAngle;
        
        
        public bool Spawned { get; private set; }
        public WeakModelRef<Location> WyrdStalker { get; private set; }
        public bool Visible { get; private set; }
        public bool WasVisible { get; private set; }
        protected Camera HeroCamera  {
            get {
                if (_camera != null) {
                    return _camera;
                }
                _camera = Hero.VHeroController.MainCamera;
                return _camera;
            }
        }
        protected HeroWyrdStalker Element { get; }
        protected bool WyrdStalkerOnScreen { get; private set; }
        protected float CameraFoVDotOuterZone { get; private set; }
        protected float CameraFoVDotThreshold { get; private set; }
        
        protected Hero Hero => Element.Hero;
        protected int SoulsPickedUpCount => Element.SoulsPickedUpCount;

        protected abstract LocationTemplate Template { get; }
        protected abstract float SpawnChance { get; }
        
        protected WyrdStalkerControllerBase(HeroWyrdStalker element) {
            Element = element;
            _nextCheckTime = Time.time + UnspawnedCheckInterval;
        }
        
        public virtual void UpdateFoV(float fov) {
            _cameraFoVAngle = Camera.VerticalToHorizontalFieldOfView(fov, HeroCamera.aspect) / 2f;
            CameraFoVDotThreshold = Mathf.Cos(_cameraFoVAngle * Mathf.Deg2Rad);
            CameraFoVDotOuterZone = Mathf.Max(CameraFoVDotThreshold, OnScreenOuterDotThreshold);
        }
        
        public void NightUpdate(float deltaTime) {
            Update(deltaTime);
            if (Time.time < _nextCheckTime) {
                return;
            }
            ControlState();
            _nextCheckTime = Spawned ? Time.time + SpawnedCheckInterval : Time.time + UnspawnedCheckInterval;
        }

        void Update(float deltaTime) {
            if (!Spawned) {
                return;
            }
            if (WyrdStalker.Get() is not { HasBeenDiscarded: false } wyrdStalker) {
                return;
            }

            WyrdStalkerOnScreen = IsOnScreen(wyrdStalker.MainView.transform);
            if (WyrdStalkerOnScreen) {
                var cameraTransform = HeroCamera.transform;
                var dotToCamera = Vector3.Dot(cameraTransform.forward, (wyrdStalker.Coords + Vector3.up * WyrdStalkerHeight - cameraTransform.position).normalized);
                HandleVisibilityGain(deltaTime, wyrdStalker, dotToCamera);
                OnScreenUpdate(deltaTime, wyrdStalker, dotToCamera);
            } else {
                HandleVisibilityLose(deltaTime);
                OffScreenUpdate(deltaTime, wyrdStalker);
            }
        }

        void HandleVisibilityGain(float deltaTime, Location wyrdStalker, float dotToCamera) {
            if (!Visible) {
                float dotToCameraMultiplier = dotToCamera.Remap(CameraFoVDotThreshold, CameraFoVDotOuterZone, 0.25f, 1f, true);
                _visibilityCoyoteTime += deltaTime * dotToCameraMultiplier;
                if (_visibilityCoyoteTime >= VisibilityCoyoteTime) {
                    Visible = true;
                    if (!WasVisible) {
                        WasVisible = true;
                        var eventReference = GameConstants.Get.wyrdStalkerOnSightAudioCue;
                        FMODManager.PlayOneShot(eventReference, wyrdStalker.Coords);
                    }
                    _visibilityCoyoteTime = VisibilityCoyoteTime;
                }
            }
        }
        
        void HandleVisibilityLose(float deltaTime) {
            if (Visible) {
                _visibilityCoyoteTime -= deltaTime * VisibilityCoyoteTimeDecreaseSpeed;
                if (_visibilityCoyoteTime <= 0f) {
                    Visible = false;
                    _visibilityCoyoteTime = 0f;
                }
            }
        }
        
        /// <summary>
        /// Checks if WyrdStalker is currently visible on screen.
        /// </summary>
        bool IsOnScreen(Transform transform) {
            const float CenterToExtentLerp = 0.5f;
            
            foreach (var renderer in transform.GetComponentsInChildren<Renderer>()) {
                var bounds = renderer.bounds;
                var maxBoundWithMargin = Vector3.Lerp(bounds.center, bounds.max, CenterToExtentLerp);
                var minBoundWithMargin = Vector3.Lerp(bounds.center, bounds.min, CenterToExtentLerp);
                var boundsVertices = new Vector3[8] {
                    new (minBoundWithMargin.x, minBoundWithMargin.y, minBoundWithMargin.z),
                    new (maxBoundWithMargin.x, minBoundWithMargin.y, minBoundWithMargin.z),
                    new (minBoundWithMargin.x, maxBoundWithMargin.y, minBoundWithMargin.z),
                    new (maxBoundWithMargin.x, maxBoundWithMargin.y, minBoundWithMargin.z),
                    new (minBoundWithMargin.x, minBoundWithMargin.y, maxBoundWithMargin.z),
                    new (maxBoundWithMargin.x, minBoundWithMargin.y, maxBoundWithMargin.z),
                    new (minBoundWithMargin.x, maxBoundWithMargin.y, maxBoundWithMargin.z),
                    new (maxBoundWithMargin.x, maxBoundWithMargin.y, maxBoundWithMargin.z)
                };
                foreach (var boundVert in boundsVertices) {
                    Vector3 viewportPosition = HeroCamera.WorldToViewportPoint(boundVert);
                    bool isVisible = viewportPosition.x is >= 0 and <= 1 &&
                                     viewportPosition.y is >= 0 and <= 1 &&
                                     viewportPosition.z > 0;
                    if (isVisible) {
                        return true;
                    }
                }
            }
            return false;
        }

        void ControlState() {
            if (Spawned) {
                TryHide();
            } else {
                TrySpawn();
            }
        }
        
        public bool TrySpawn(bool ignoreRequirements = false) {
            if (ignoreRequirements || (ShouldSpawn() && RandomUtil.WithProbability(SpawnChance))) {
                return Spawn() != null;
            }

            return false;
        }

        Location Spawn() {
            if (!TryFindValidSpawnPoint(out var spawnPoint)) {
                return null;
            }

            var wyrdStalker = SpawnInternal(Template, spawnPoint);
            if (wyrdStalker is { HasBeenDiscarded: false }) {
                var eventReference = GameConstants.Get.wyrdStalkerSpawnAudioCue;
                FMODManager.PlayOneShot(eventReference, spawnPoint);
                OnSpawned(wyrdStalker);
            }

            return wyrdStalker;
        }
        
        /// <summary>
        /// Finds a spawn point that is just outside of the hero's FoV and Hero is visible from that point.
        /// </summary>
        bool TryFindValidSpawnPoint(out Vector3 spawnPoint) {
            var heroForward = Hero.Forward();
            var heroCoords = Hero.Coords;
            var heroHeadPosition = Hero.Head.position;
            
            for (int i = 0; i < MaxAngleTries; i++) {
                bool useLeftSide = RandomUtil.WithProbability(0.5f);
                var randomAngle = Random.Range(_cameraFoVAngle, _cameraFoVAngle + AcceptableAngleThreshold);
                if (useLeftSide) {
                    randomAngle *= -1f;
                }
                
                var searchForward = Quaternion.AngleAxis(randomAngle, Vector3.up) * heroForward;
                for (int j = 0; j < MaxRangeTries; j++) {
                    spawnPoint = heroCoords + searchForward * WyrdStalkerSpawnRange.RandomPick();
                    spawnPoint = Ground.SnapToGround(spawnPoint, findClosest: false);
                    
                    if (AIUtils.CanSee(spawnPoint + Vector3.up * WyrdStalkerHeight, heroHeadPosition)) {
                        var spawnPointNode = AstarPath.active.GetNearest(spawnPoint).node;
                        var heroPositionNode = AstarPath.active.GetNearest(heroCoords).node;
                        if (PathUtilities.IsPathPossible(spawnPointNode, heroPositionNode)) {
                            return true;
                        }
                    }
                }
            }

            spawnPoint = Vector3.zero;
            return false;
        }

        Location SpawnInternal(LocationTemplate template, Vector3 spawnPoint) {
            var wyrdStalker = template.SpawnLocation(spawnPoint, Quaternion.LookRotation(Hero.Coords - spawnPoint, Vector3.up));
            wyrdStalker.MarkedNotSaved = true;
            
            if (CommonReferences.Get.wyrdStalkerShowVFX is { IsSet: true } vfx) {
                PrefabPool.InstantiateAndReturn(vfx, wyrdStalker.Coords, wyrdStalker.Rotation).Forget();
            }
            
            WyrdStalker = wyrdStalker;
            Spawned = true;
            _nextCheckTime = Time.time + SpawnedCheckInterval;
            Log.Minor?.Info($"Wyrd Stalker spawned!");
            return wyrdStalker;
        }

        bool TryHide() {
            if (WyrdStalker.Get() is not { HasBeenDiscarded: false }) {
                Log.Minor?.Info("WyrdStalker Hid: It is discarded");
                HideWyrdStalker();
                return true;
            }
            
            if (!Element.IsHeroInWyrdness) {
                Log.Minor?.Info("WyrdStalker Hid: Hero not in Wyrdness");
                HideWyrdStalker();
                return true;
            }

            if (ShouldHide()) {
                HideWyrdStalker();
                return true;
            }

            return false;
        }

        public void HideWyrdStalker() {
            if (WyrdStalker.Get() is { HasBeenDiscarded: false } wyrdStalker) {
                Log.Minor?.Info($"Wyrd Stalker hidden!");
                if (WyrdStalkerOnScreen) {
                    HideWhenOnScreen(wyrdStalker).Forget();
                } else {
                    wyrdStalker.Discard();
                }
            }
            
            WyrdStalker = null;
            Spawned = false;
            WasVisible = false;
            Visible = false;
            _visibilityCoyoteTime = 0f;
            _nextCheckTime = Time.time + AfterHiddenNextShowCooldown;
            OnHidden();
        }

        static async UniTaskVoid HideWhenOnScreen(Location wyrdStalker) {
            var eventReference = GameConstants.Get.wyrdStalkerHideAudioCue;
            FMODManager.PlayOneShot(eventReference, wyrdStalker.Coords);
            
            if (CommonReferences.Get.wyrdStalkerHideVFX is { IsSet: true } vfx) {
                PrefabPool.InstantiateAndReturn(vfx, wyrdStalker.Coords, wyrdStalker.Rotation).Forget();
            }
            
            if (!await AsyncUtil.DelayTime(wyrdStalker, 0.5f)) {
                return;
            }
            
            wyrdStalker.Discard();
        }
        
        protected abstract void OnScreenUpdate(float deltaTime, Location wyrdStalker, float dotToCamera);
        protected abstract void OffScreenUpdate(float deltaTime, Location wyrdStalker);
        protected abstract bool ShouldSpawn();
        protected abstract void OnSpawned(Location wyrdStalker); 
        protected abstract bool ShouldHide();
        protected abstract void OnHidden();
    }
}