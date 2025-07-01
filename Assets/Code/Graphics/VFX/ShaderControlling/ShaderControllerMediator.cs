using System;
using System.Collections.Generic;
using Awaken.Utility.Collections;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Awaken.TG.Graphics.VFX.ShaderControlling {
    public class ShaderControllerMediator : MonoBehaviour, IVFXOnStopEffects {
        [SerializeField] SimpleShaderController[] _shaderControllers = Array.Empty<SimpleShaderController>();
        [SerializeField] AdvancedShaderController[] _advancedShaderControllers = Array.Empty<AdvancedShaderController>();
        [SerializeField] bool _listenForVFXStopped = true;
        [SerializeField, ShowIf(nameof(_listenForVFXStopped))] bool _reverseStopEffect = true;

        public void VFXStopped() { 
            if (_listenForVFXStopped) {
                StartEffectForAll(!_reverseStopEffect);
            }
        }

        public void SetDuration(float duration) {
            foreach (var simpleShaderController in _shaderControllers) {
                simpleShaderController.Duration = duration;
            }
            foreach (var advancedShaderController in _advancedShaderControllers) {
                advancedShaderController.Duration = duration;
            }
        }

        public void StartEffectForAll(bool forward) {
            foreach (var simpleShaderController in _shaderControllers) {
                simpleShaderController.StartEffect(forward);
            }
            foreach (var advancedShaderController in _advancedShaderControllers) {
                advancedShaderController.StartEffect(forward);
            }
        }

        public void StartEffectForAll() => StartEffectForAll(true);
        public void StopEffectForAll() => StartEffectForAll(false);
    }
}