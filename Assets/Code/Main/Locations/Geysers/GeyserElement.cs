using System.Threading;
using Awaken.TG.Main.AudioSystem;
using Awaken.TG.Main.Fights.Utils;
using Awaken.TG.Main.Locations.Attachments;
using Awaken.TG.Main.Timing;
using Awaken.TG.MVC;
using Awaken.TG.MVC.Elements;
using Awaken.Utility;
using Awaken.Utility.GameObjects;
using Cysharp.Threading.Tasks;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.VFX;

namespace Awaken.TG.Main.Locations.Geysers {
    public partial class GeyserElement : Element<Location>, IRefreshedByAttachment<GeyserAttachment> {
        public override ushort TypeForSerialization => SavedModels.GeyserElement;

        GeyserAttachment _spec;
        double _nextActionTime;
        CancellationTokenSource _cts;
        GeyserDataMarker _data;
        
        Transform GeyserTop => _data.top;
        VisualEffect GeyserVFX => _data.vfx;
        ARFmodEventEmitter GroundEmitterIdle => _data.groundEmitterIdle;
        ARFmodEventEmitter GroundEmitterActive => _data.groundEmitterActive;
        ARFmodEventEmitter TopEmitterActive => _data.topEmitterActive;
        ARFmodEventEmitter InsideEmitterActive => _data.insideEmitterActive;
        
        public void InitFromAttachment(GeyserAttachment spec, bool isRestored) {
            _spec = spec;
        }

        protected override void OnInitialize() {
            ParentModel.OnVisualLoaded(OnVisualLoaded);
        }
        
        void OnVisualLoaded(Transform transform) {
            _data = transform.GetComponentInChildren<GeyserDataMarker>(true);
            
            _nextActionTime = World.Any<GameRealTime>().PlayRealTimeInSeconds;
            var lifetimeDuration = _nextActionTime;
            lifetimeDuration -= _spec.firstUseDelay;
            if (lifetimeDuration <= 0) {
                GeyserEventService.GetOrCreate().RegisterGeyser(GetEvent(-lifetimeDuration, true));
                return;
            }
            lifetimeDuration %= _spec.activeTime + _spec.inactiveTime;
            lifetimeDuration -= _spec.activeTime;
            if (lifetimeDuration < 0) {
                ActivateInternal(true);
                GeyserEventService.GetOrCreate().RegisterGeyser(GetEvent(-lifetimeDuration, false));
                return;
            }
            GeyserEventService.GetOrCreate().RegisterGeyser(GetEvent(_spec.inactiveTime - lifetimeDuration, true));
        }

        GeyserEventService.GeyserEvent GetEvent(double duration, bool activate) {
            _nextActionTime += duration;
            return new GeyserEventService.GeyserEvent(this, _nextActionTime, activate);
        }

        public GeyserEventService.GeyserEvent ActivateAndGetNextEvent(bool instant) {
            ActivateInternal(instant);
            return GetEvent(_spec.activeTime, false);
        }
        
        public GeyserEventService.GeyserEvent DeactivateAndGetNextEvent(bool instant) {
            DeactivateInternal(instant);
            return GetEvent(_spec.inactiveTime, true);
        }
        
        public async UniTask DeactivateWhenHidden() {
            if (!await AsyncUtil.WaitUntil(this, () => GeyserTop.localPosition.y == 0)) {
                return;
            }
            GeyserEventService.TryGet()?.UnregisterGeyser(this);
        }
        
        public async UniTask ActivateWithDelay(float delay) {
            if (!await AsyncUtil.DelayTime(this, delay)) {
                return;
            }
            await ActivateInternal();
            if (HasBeenDiscarded) {
                return;
            }
            if (GeyserEventService.TryGet()?.IsGeyserRegistered(this) == true) {
                return;
            }
            _nextActionTime = World.Any<GameRealTime>().PlayRealTimeInSeconds;
            GeyserEventService.GetOrCreate()?.RegisterGeyser(GetEvent(_spec.activeTime, false));
        }

        UniTask ActivateInternal(bool forceInstant = false) {
            UniTask task = UniTask.CompletedTask;
            
            GeyserTop.TrySetActiveOptimized(true);
            if (GeyserVFX != null) {
                GeyserVFX.gameObject.SetActive(true);
            }
            if (forceInstant || _spec.raiseDuration <= 0f) {
                MoveTargetToHeightInstant(_spec.height, true);
            } else {
                task = MoveTargetToHeightSmooth(_spec.height, true, _spec.raiseDuration);
            }
            if (GroundEmitterIdle != null) {
                // GroundEmitterIdle.Stop();
            }
            if (GroundEmitterActive != null) {
                // GroundEmitterActive.Play();
            }
            if (TopEmitterActive != null) {
                // TopEmitterActive.Play();
            }
            if (GeyserVFX != null) {
                GeyserVFX.Play();
            }
            
            return task;
        }

        UniTask DeactivateInternal(bool forceInstant = false) {
            UniTask task = UniTask.CompletedTask;
            
            if (forceInstant || _spec.dropDuration <= 0f) {
                MoveTargetToHeightInstant(0, false);
            } else {
                task = MoveTargetToHeightSmooth(0, false, _spec.dropDuration);
            }
            if (GroundEmitterIdle != null) {
                // GroundEmitterIdle.Play();
            }
            if (GroundEmitterActive != null) {
                // GroundEmitterActive.Stop();
            }
            if (TopEmitterActive != null) {
                // TopEmitterActive.Stop();
            }
            if (InsideEmitterActive != null) {
                // InsideEmitterActive.Stop();
            }
            if (GeyserVFX != null) {
                GeyserVFX.Stop();
            }
            return task;
        }

        void MoveTargetToHeightInstant(float height, bool activityAtEnd) {
            _cts?.Cancel();
            _cts = null;
            
            Vector3 currentPos = GeyserTop.localPosition;
            currentPos.y = height;
            GeyserTop.localPosition = currentPos;
            GeyserTop.TrySetActiveOptimized(activityAtEnd);
            if (GeyserVFX != null) {
                GeyserVFX.gameObject.SetActive(activityAtEnd);
            }
        }
        
        async UniTask MoveTargetToHeightSmooth(float height, bool activityAtEnd, float duration) {
            _cts?.Cancel();
            _cts = new CancellationTokenSource();
            
            Vector3 currentPos = GeyserTop.localPosition;
            float startingY = currentPos.y;
            float elapsed = 0f;
            bool hasNoVFX = GeyserVFX == null;
            while (elapsed < duration && (hasNoVFX || GeyserVFX.enabled)) {
                elapsed += Time.deltaTime;
                currentPos.y = math.lerp(startingY, height, elapsed / duration);
                GeyserTop.localPosition = currentPos;
                if (!await AsyncUtil.DelayFrame(this, 1, _cts.Token)) {
                    return;
                }
            }
            currentPos.y = height;
            GeyserTop.localPosition = currentPos;
            GeyserTop.TrySetActiveOptimized(activityAtEnd);
            
            if (GeyserVFX != null) {
                duration = 5;
                elapsed = 0;
                while (elapsed < duration && (hasNoVFX || GeyserVFX.enabled)) {
                    elapsed += Time.deltaTime;
                    if (!await AsyncUtil.DelayFrame(this, 1, _cts.Token)) {
                        return;
                    }
                }
                GeyserVFX.gameObject.SetActive(activityAtEnd);
            }
        }
        
        protected override void OnDiscard(bool fromDomainDrop) {
            GeyserEventService.TryGet()?.UnregisterGeyser(this);
        }
    }
}
