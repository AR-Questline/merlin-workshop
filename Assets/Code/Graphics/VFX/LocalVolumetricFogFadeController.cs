using Awaken.TG.Main.Utility.RichEnums;
using Awaken.TG.Utility;
using UnityEngine;
using UnityEngine.Rendering.HighDefinition;

namespace Awaken.TG.Graphics.VFX {
    public class LocalVolumetricFogFadeController : MonoBehaviour, IVFXOnStopEffects {
        
        [SerializeField] float fadeInDuration = 1f;
        [SerializeField] float fadeInTargetFogDistance = 1f;
        [SerializeField] float fadeOutDuration = 1f;
        
        [SerializeField] bool fadeInOnEnable;
        [SerializeField] bool fadeOutOnVfxStop;
        
        [SerializeField, RichEnumExtends(typeof(EasingType))] RichEnumReference fadeEasingType;

        LocalVolumetricFog _controlledFog;
        float _currentPercentage;
        float _targetPercentage;
        float _fadeDuration;
        float _targetFogDistance;
        
        EasingType FadeEasingType => fadeEasingType.EnumAs<EasingType>();
        bool IsFading => _currentPercentage != _targetPercentage;
        
        void Awake() {
            _controlledFog = GetComponent<LocalVolumetricFog>();
        }
        
        void OnEnable() {
            if (fadeInOnEnable) {
                FadeIn();
            }
        }
        
        public void VFXStopped() {
            if (fadeOutOnVfxStop) {
                FadeOut();
            }
        }
        
        public void FadeIn() {
            _currentPercentage = 0.0f;
            _targetPercentage = 1.0f;
            _fadeDuration = fadeInDuration;
            _targetFogDistance = fadeInTargetFogDistance;
        }
        
        public void FadeOut() {
            _targetPercentage = 0.0f;
            _currentPercentage = 1.0f;
            _fadeDuration = fadeOutDuration;
            _targetFogDistance = _controlledFog.parameters.meanFreePath;
        }

        void Update() {
            if (_controlledFog == null) {
                return;
            }
            
            if (IsFading) {
                _currentPercentage = Mathf.MoveTowards(_currentPercentage, _targetPercentage, Time.deltaTime / _fadeDuration);
                SetFogDistancePercentage(_currentPercentage);
            }
        }

        void SetFogDistancePercentage(float percentage) {
            float easedPercentage = FadeEasingType?.Calculate(percentage) ?? percentage;
            float fogDistance = Mathf.Infinity;
            if (easedPercentage > 0.0f) {
                fogDistance = 1.0f / easedPercentage * _targetFogDistance;
            }

            _controlledFog.parameters.meanFreePath = fogDistance;
        }
    }
}