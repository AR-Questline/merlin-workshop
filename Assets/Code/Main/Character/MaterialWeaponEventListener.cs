using System.Threading;
using Awaken.Kandra;
using Awaken.TG.Main.Fights.Utils;
using Awaken.TG.MVC;
using Awaken.TG.MVC.Events;
using Cysharp.Threading.Tasks;
using Unity.Mathematics;
using UnityEngine;

namespace Awaken.TG.Main.Character { 
    public class MaterialWeaponEventListener<T> where T : Object, IMaterialWeaponEventListenerProvider, IListenerOwner {
        readonly T _provider;
        readonly IAlive _target;
        readonly KandraRenderer _rendererWithMaterial;

        CancellationTokenSource _cts;
        IEventListener _deathListener;
        Material _instancedMaterial;
        bool _active;
        
        public MaterialWeaponEventListener(T provider, IAlive target, KandraRenderer rendererWithMaterial) {
            _provider = provider;
            _target = target;
            _rendererWithMaterial = rendererWithMaterial;
        }

        public void Activate() {
            if (_active) {
                return;
            }
            _active = true;
            if (_rendererWithMaterial == null) {
                return;
            }
            _deathListener = _target?.ListenTo(IAlive.Events.BeforeDeath, Deactivate, _provider);
            ActivateTween(_provider.ValueActivated, _provider.Parameter, _provider.LerpTime).Forget();
        }
        
        public void Deactivate() {
            if (!_active) {
                return;
            }
            _active = false;
            if (_rendererWithMaterial == null) {
                return;
            }
            World.EventSystem.TryDisposeListener(ref _deathListener);
            DeactivateTween(_provider.ValueDeactivated, _provider.Parameter, _provider.LerpTime).Forget();
        }
        
        public void OnDiscard() {
            _cts?.Cancel();
            World.EventSystem.TryDisposeListener(ref _deathListener);
        }

        async UniTaskVoid ActivateTween(float valueTo, string parameter, float lerpTime) {
            _cts?.Cancel();
            _cts = new CancellationTokenSource();
            
            if (_instancedMaterial == null) {
                if (!await AsyncUtil.WaitForPlayerLoopEvent(_provider, PlayerLoopTiming.Update, _cts)) {
                    return;
                }
                _instancedMaterial = _rendererWithMaterial.UseInstancedMaterial(_provider.MaterialIndex);
            }
            
            float valueFrom = _instancedMaterial.GetFloat(parameter);
            float currentTime = 0f;
            
            _cts = new CancellationTokenSource();
            while (await AsyncUtil.DelayFrame(_provider, 1, _cts.Token)) {
                currentTime += Time.deltaTime;
                if (currentTime >= lerpTime) {
                    _instancedMaterial.SetFloat(parameter, valueTo);
                    break;
                }
                _instancedMaterial.SetFloat(parameter, math.lerp(valueFrom, valueTo, currentTime / lerpTime));
            }
        }

        async UniTaskVoid DeactivateTween(float valueTo, string parameter, float lerpTime) {
            _cts?.Cancel();
            _cts = new CancellationTokenSource();
            
            float valueFrom = _instancedMaterial.GetFloat(parameter);
            float currentTime = 0f;
            while (await AsyncUtil.DelayFrame(_provider, 1, _cts.Token)) {
                currentTime += Time.deltaTime;
                if (currentTime >= lerpTime) {
                    break;
                }
                _instancedMaterial.SetFloat(parameter, math.lerp(valueFrom, valueTo, currentTime / lerpTime));
            }
            
            _rendererWithMaterial.UseOriginalMaterial(_provider.MaterialIndex);
            _instancedMaterial = null;
        }
    }
}
