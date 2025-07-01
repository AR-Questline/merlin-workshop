using Awaken.Utility.Debugging;
using DG.Tweening;
using UnityEngine;

namespace Awaken.TG.Graphics.VFX.ShaderControlling {
    public class SimpleShaderFloatController : SimpleShaderController {
        [SerializeField] bool _loop;
        [SerializeField] bool _pingPong;

        protected override void DoStartEffect(bool positiveDirection) {
            DOTween.Kill(this);
            var target = positiveDirection ? _defaultValue : _targetValue;
            for (int i = 0; i < _materials.Count; i++) {
                _materials[i].SetFloat(_shaderPropertyId, target);
            }
            target = positiveDirection ? _targetValue :  _defaultValue;
            for (int i = 0; i < _materials.Count; i++) {
                if (_useCurve) {
                    _materials[i]
                        .DOFloat(target, _shaderPropertyId, Duration)
                        .SetEase(_curve)
                        .SetUpdate(_useUnscaledTime)
                        .SetId(this)
                        .OnComplete(() => OnTweenCompleted(positiveDirection));                    
                } else {
                    _materials[i]
                        .DOFloat(target, _shaderPropertyId, Duration)
                        .SetEase(_ease)
                        .SetUpdate(_useUnscaledTime)
                        .SetId(this)
                        .OnComplete(() => OnTweenCompleted(positiveDirection));   
                }
            }
        }

        void OnTweenCompleted(bool wasEven) {
            if (_loop && gameObject.activeInHierarchy) {
                if (_pingPong) {
                    StartEffect(!wasEven);
                } else {
                    for (int i = 0; i < _materials.Count; i++) {
                        _materials[i].SetFloat(_shaderPropertyId, _defaultValue);
                    }

                    StartEffect(wasEven);
                }
            }
        }

        protected override void ResetValues() {
            base.ResetValues();
            DOTween.Kill(this);
            DOTween.Kill(_delayTweenID);
            for (int i = 0; i < _materials.Count; i++) {
                var material = _materials[i];
                if (material == null) {
#if AR_DEBUG || DEBUG
                    Log.Important?.Error($"Null material in simple shader float controller - {gameObject.name}");
#endif
                    continue;
                }

                material.SetFloat(_shaderPropertyId, _defaultValue);
            }
        }
    }
}