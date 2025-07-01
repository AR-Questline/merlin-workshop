using DG.Tweening;
using UnityEngine;

namespace Awaken.TG.Graphics.VFX.ShaderControlling {
    public class SimpleShaderPeriodicRandomFloat : SimpleShaderController {
        [SerializeField] Vector2 _randomMinMax = new(0, 50);
        
        Tween _tween;
        
        protected override void DoStartEffect(bool positiveDirection) {
            _tween.Kill();
            float randomFloat = Random.Range(_randomMinMax.x, _randomMinMax.y);
            for (int i = 0; i < _materials.Count; i++) {
                _materials[i].SetFloat(_shaderPropertyId, randomFloat);
            } 

            _tween = DOVirtual.DelayedCall(Duration, () => DoStartEffect(true)).SetUpdate(_useUnscaledTime);
        }

        protected override void ResetValues() {
            base.ResetValues();
            _tween.Kill();
            for (int i = 0; i < _materials.Count; i++) {
                _materials[i].SetFloat(_shaderPropertyId, _defaultValue);
            }
        }
    }
}