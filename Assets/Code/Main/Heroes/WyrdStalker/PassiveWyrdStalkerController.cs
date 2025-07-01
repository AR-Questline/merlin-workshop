using Awaken.TG.Main.Character;
using Awaken.TG.Main.Fights.NPCs;
using Awaken.TG.Main.General;
using Awaken.TG.Main.General.Configs;
using Awaken.TG.Main.Locations;
using Awaken.TG.Main.Locations.Setup;
using Awaken.TG.Main.Scenes.SceneConstructors;
using Awaken.TG.MVC;
using Awaken.TG.Utility;
using Awaken.Utility.Debugging;
using UnityEngine;

namespace Awaken.TG.Main.Heroes.WyrdStalker {
    internal sealed class PassiveWyrdStalkerControllerBase : WyrdStalkerControllerBase {
        const float OnScreenInnerDotThreshold = 0.866f;
        
        static FloatRange AliveRangeSqr => GameConstants.Get.passiveWyrdStalkerAliveRangeSqr;
        static float TooLongVisibleTimeout => GameConstants.Get.tooLongVisibleTimeout;
        static float TooLongVisibleCoyoteTimeDecreaseSpeed => GameConstants.Get.tooLongVisibleCoyoteTimeDecreaseSpeed;
        

        float _cameraFoVDotInnerZone = OnScreenInnerDotThreshold;
        
        float _tooLongVisibleTimer;
        
        protected override LocationTemplate Template => CommonReferences.Get.WyrdStalkerPassiveTemplate.Get<LocationTemplate>();
        protected override float SpawnChance => GameConstants.Get.wyrdStalkerPassiveSpawnChance;

        public PassiveWyrdStalkerControllerBase(HeroWyrdStalker element) : base(element) {
            UpdateFoV(HeroCamera.fieldOfView);
        }

        public override void UpdateFoV(float fov) {
            base.UpdateFoV(fov);
            _cameraFoVDotInnerZone = Mathf.Max(CameraFoVDotThreshold, OnScreenInnerDotThreshold);
        }

        protected override void OnScreenUpdate(float deltaTime, Location wyrdStalker, float dotToCamera) {
            if (dotToCamera > CameraFoVDotOuterZone) {
                float dotToCameraMultiplier = dotToCamera.Remap(CameraFoVDotThreshold, _cameraFoVDotInnerZone, 0f, 1f, true);
                _tooLongVisibleTimer += deltaTime * dotToCameraMultiplier;
            } else {
                _tooLongVisibleTimer -= deltaTime * TooLongVisibleCoyoteTimeDecreaseSpeed;
                if (_tooLongVisibleTimer <= 0f) {
                    _tooLongVisibleTimer = 0f;
                }
            }
            wyrdStalker.SafelyRotateTo(Quaternion.LookRotation(Hero.Coords - wyrdStalker.Coords, Vector3.up));
        }

        protected override void OffScreenUpdate(float deltaTime, Location wyrdStalker) {
            _tooLongVisibleTimer = 0f;
            wyrdStalker.SafelyRotateTo(Quaternion.LookRotation(Hero.Coords - wyrdStalker.Coords, Vector3.up));
        }

        protected override bool ShouldSpawn() {
            if (!Element.IsHeroInWyrdness) return false;
            if (SoulsPickedUpCount >= HeroWyrdStalker.ActiveWyrdStalkerThreshold) {
                Element.RefreshController();
                return false;
            }
            return true;
        }
        
        protected override void OnSpawned(Location wyrdStalker) {
            wyrdStalker.TryGetElement<NpcElement>()?.HealthElement?.ListenTo(HealthElement.Events.OnDamageTaken, HideWyrdStalker, wyrdStalker);
        }

        protected override bool ShouldHide() {
            float sqrDistance = (WyrdStalker.Get().Coords - Hero.Coords).sqrMagnitude;
            if (sqrDistance > AliveRangeSqr.max) {
                Log.Minor?.Info("WyrdStalker Hid: Hero too far away");
                return true;
            }
            
            if (sqrDistance < AliveRangeSqr.min) {
                Log.Minor?.Info("WyrdStalker Hid: Hero too close");
                return true;
            }
            
            if (WasVisible && !Visible) {
                Log.Minor?.Info("WyrdStalker Hid: Was on screen and now isn't");
                return true;
            }
            
            if (_tooLongVisibleTimer > TooLongVisibleTimeout) {
                Log.Minor?.Info("WyrdStalker Hid: Looking at it for too long");
                return true;
            }

            return false;
        }

        protected override void OnHidden() {
            _tooLongVisibleTimer = 0f;
        }
    }
}