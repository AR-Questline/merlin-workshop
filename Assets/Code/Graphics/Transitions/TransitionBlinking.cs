using System;
using Awaken.TG.Main.Cameras;
using Awaken.TG.MVC;
using DG.Tweening;
using UnityEngine;
using UnityEngine.Rendering.HighDefinition;

namespace Awaken.TG.Graphics.Transitions {
    public class TransitionBlinking : MonoBehaviour {
        [SerializeField] RectTransform eyeLidTopParent;
        [SerializeField] RectTransform eyeLidTop;
        [SerializeField] RectTransform eyeLidBottomParent;
        [SerializeField] RectTransform eyeLidBottom;

        [Header("Blur")]
        [SerializeField] CustomPassVolume customPassBlur;
        static readonly int Blur = Shader.PropertyToID("_Blur");

        bool _initialized;
        bool _isBlinking;
        float _halfHeight;
        GaussianBlur _cachedGaussianBlur;
        Sequence _blinkingSequence;
        
        public bool IsBlinking => _isBlinking;
        
        void OnDisable() {
            ResetBlinking();
        }
        
        void Init() {
            _halfHeight = Screen.height / 2f;

            var halfVector = new Vector2(0, _halfHeight);
            eyeLidTopParent.sizeDelta = halfVector;
            eyeLidTop.sizeDelta = halfVector;
            eyeLidBottomParent.sizeDelta = halfVector;
            eyeLidBottom.sizeDelta = halfVector;
            
            if (customPassBlur.customPasses[0] is GaussianBlur gaussianBlur) {
                _cachedGaussianBlur = gaussianBlur;
                _cachedGaussianBlur.enabled = false;
                gaussianBlur.radius = 0;
            }

            _initialized = true;
        }

        public void Blink(Data data) {
            customPassBlur.targetCamera = World.Only<GameCamera>().MainCamera;
            if (!_initialized) {
                Init();
            }
            ResetBlinking();
            StartBlinking(data);
        }

        public void Blink(Data data, Camera camera) {
            customPassBlur.targetCamera = camera;
            if (!_initialized) {
                Init();
            }
            ResetBlinking();
            StartBlinking(data);
        }
        
        public void ResetBlinking() {
            if (!_isBlinking) {
                return;
            }
            StopBlinking();
        }

        void StartBlinking(Data data) {
            _isBlinking = true;
            _cachedGaussianBlur.enabled = true;
            
            _blinkingSequence.Kill();
            _blinkingSequence = DOTween.Sequence().SetUpdate(!data.isTimeScaleDependent);
            
            _blinkingSequence.Append(DOTween.To(() => 0f, percent => ProcessSamplingCurve(data, percent), 1f, data.duration).OnKill(ResetBlinking).OnComplete(StopBlinking));
        }

        void StopBlinking() {
            eyeLidBottom.sizeDelta = Vector2.zero;
            eyeLidBottomParent.sizeDelta = Vector2.zero;
            eyeLidTop.sizeDelta = Vector2.zero;
            eyeLidTopParent.sizeDelta = Vector2.zero;
            
            if (customPassBlur.customPasses[0] is FullScreenCustomPass fullScreenPass) {
                fullScreenPass.fullscreenPassMaterial.SetFloat(Blur, 0);
            }
            
            _isBlinking = false;
            _cachedGaussianBlur.enabled = false;
            
            _blinkingSequence.Kill();
            _blinkingSequence = null;
        }

        void ProcessSamplingCurve(Data data, float percent) {
            UpdateEffect(data.blinkingCurve.Evaluate(percent), 
                data.eyeLidAnimation.Evaluate(percent), 
                data.targetBlurStrength * (1 - data.blurCurve.Evaluate(percent)));
        }

        void UpdateEffect(float evaluatedBlinkingCurve, float evaluatedEyeLidCurve, float blurStrength) {
            var childHeight =  _halfHeight * (evaluatedBlinkingCurve * 0.4f) * evaluatedEyeLidCurve;
            var parentHeight = Mathf.Lerp(_halfHeight, 0, evaluatedBlinkingCurve);
            var childVector = new Vector2(0, childHeight);
            var parentVector = new Vector2(0, parentHeight);
            eyeLidTop.sizeDelta = childVector;
            eyeLidBottom.sizeDelta = childVector;
            eyeLidTopParent.sizeDelta = parentVector;
            eyeLidBottomParent.sizeDelta = parentVector;

            if (!ReferenceEquals(_cachedGaussianBlur, null)) {
                if (_cachedGaussianBlur.radius >= 0 && blurStrength <= 0) {
                    _cachedGaussianBlur.enabled = false;
                }
                else if (_cachedGaussianBlur.radius <= 0 && blurStrength > 0) {
                    _cachedGaussianBlur.enabled = true;
                }
                _cachedGaussianBlur.radius = blurStrength;
            }
        }
        
        [Serializable]
        public struct Data {
            public float duration;
            public AnimationCurve eyeLidAnimation;
            public AnimationCurve blinkingCurve;
            public AnimationCurve blurCurve;
            public float targetBlurStrength;
            public bool isTimeScaleDependent;
        }
    }
}
