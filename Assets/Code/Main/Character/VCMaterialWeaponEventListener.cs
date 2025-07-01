using System.Threading;
using Awaken.Kandra;
using Awaken.TG.Main.Fights.Utils;
using Awaken.TG.Main.Locations;
using Awaken.TG.Main.Utility.Animations;
using Awaken.TG.MVC;
using Awaken.TG.MVC.Events;
using Cysharp.Threading.Tasks;
using JetBrains.Annotations;
using Unity.Mathematics;
using UnityEngine;

namespace Awaken.TG.Main.Character {
    public class VCMaterialWeaponEventListener : ViewComponent<Location> {
        [SerializeField] ARAnimationEvent.ActionType activateEvent;
        [SerializeField] ARAnimationEvent.ActionType deactivateEvent;
        [SerializeField] KandraRenderer rendererWithMaterial;
        [SerializeField] int materialIndex;
        [SerializeField] string parameter;
        [SerializeField] float valueActivated;
        [SerializeField] float valueDeactivated;
        [SerializeField] float lerpTime;
        
        int _lastAnimationEventFrame;
        Object _lastAnimationEventObject;
        CancellationTokenSource _cts;
        IEventListener _deathListener;
        Material _instancedMaterial;
        bool _active;
        
        protected override void OnAttach() { }
        
        // --- Called from animator event
        [UsedImplicitly, UnityEngine.Scripting.Preserve]
        void TriggerAnimationEvent(Object obj) {
            if (Target is not { HasBeenDiscarded: false }) {
                return;
            }

            // --- Head animations use the same animation as the weapons, so we need to filter out events from those animations.
            if (_lastAnimationEventFrame == Time.frameCount && obj == _lastAnimationEventObject) {
                return;
            }
            _lastAnimationEventFrame = Time.frameCount;
            _lastAnimationEventObject = obj;

            if (obj is ARAnimationEvent animationEvent) {
                // --- Actions
                if (animationEvent.actionType == activateEvent) {
                    Activate();
                } else if (animationEvent.actionType == deactivateEvent) {
                    Deactivate();
                }
            }
        }

        void Activate() {
            if (_active) {
                return;
            }
            _active = true;
            _deathListener = Target.TryGetElement<IAlive>()?.ListenTo(IAlive.Events.BeforeDeath, Deactivate, this);
            ActivateTween(valueActivated, parameter, lerpTime).Forget();
        }

        async UniTaskVoid ActivateTween(float valueTo, string parameter, float lerpTime) {
            _cts?.Cancel();
            _cts = new CancellationTokenSource();
            
            if (_instancedMaterial == null) {
                if (!await AsyncUtil.WaitForPlayerLoopEvent(this, PlayerLoopTiming.Update, _cts)) {
                    return;
                }
                _instancedMaterial = rendererWithMaterial.UseInstancedMaterial(materialIndex);
            }
            
            float valueFrom = _instancedMaterial.GetFloat(parameter);
            float currentTime = 0f;
            
            _cts = new CancellationTokenSource();
            while (await AsyncUtil.DelayFrame(this, 1, _cts.Token)) {
                currentTime += Time.deltaTime;
                if (currentTime >= lerpTime) {
                    _instancedMaterial.SetFloat(parameter, valueTo);
                    break;
                }
                _instancedMaterial.SetFloat(parameter, math.lerp(valueFrom, valueTo, currentTime / lerpTime));
            }
        }
        
        void Deactivate() {
            if (!_active) {
                return;
            }
            _active = false;
            World.EventSystem.TryDisposeListener(ref _deathListener);
            DeactivateTween(valueDeactivated, parameter, lerpTime).Forget();
        }
        
        async UniTaskVoid DeactivateTween(float valueTo, string parameter, float lerpTime) {
            _cts?.Cancel();
            _cts = new CancellationTokenSource();
            
            float valueFrom = _instancedMaterial.GetFloat(parameter);
            float currentTime = 0f;
            while (await AsyncUtil.DelayFrame(this, 1, _cts.Token)) {
                currentTime += Time.deltaTime;
                if (currentTime >= lerpTime) {
                    break;
                }
                _instancedMaterial.SetFloat(parameter, math.lerp(valueFrom, valueTo, currentTime / lerpTime));
            }
            
            rendererWithMaterial.UseOriginalMaterial(materialIndex);
            _instancedMaterial = null;
        }

        protected override void OnDiscard() {
            _cts?.Cancel();
            World.EventSystem.TryDisposeListener(ref _deathListener);
        }
    }
}
