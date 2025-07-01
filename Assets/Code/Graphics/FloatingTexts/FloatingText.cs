using Awaken.TG.Main.AudioSystem;
using Awaken.TG.Main.Cameras;
using Awaken.TG.Main.General;
using Awaken.TG.MVC;
using Awaken.TG.Utility;
using DG.Tweening;
using TMPro;
using UnityEngine;

namespace Awaken.TG.Graphics.FloatingTexts {
    /// <summary>
    /// Proxy that allows setup text and animations for floating text object
    /// All done with Fluent interface
    /// </summary>
    public class FloatingText : MonoBehaviour {
        // === Fields
        public float frequency = 0.01f;

        Sequence _sequence;
        RectTransform _rectTransform;
        
        public float Progress => _sequence.position / _sequence.Duration();
        TextMeshProUGUI TextField => GetComponentInChildren<TextMeshProUGUI>();
        CanvasGroup TextCanvasGroup => TextField.GetComponent<CanvasGroup>();

        // === Initialization and text
        public FloatingText Init(string text, Vector2 position) {
            TextField.text = text;
            _rectTransform = GetComponent<RectTransform>();
            _rectTransform.position = position;
            
            _sequence = DOTween.Sequence();
            return this;
        }

        void Update() {
            Vector3 anchoredPosition = _rectTransform.anchoredPosition3D;
            anchoredPosition.z = 0f;
            _rectTransform.anchoredPosition3D = anchoredPosition;
        }

        public void AppendText(string text) {
            TextField.text += "\n" + text;
        }

        [UnityEngine.Scripting.Preserve]
        public FloatingText SetColor(Color color) {
            TextMeshProUGUI textMeshPro = TextField;
            textMeshPro.text = textMeshPro.text.ColoredText(color);
            return this;
        }
        
        [UnityEngine.Scripting.Preserve]
        public FloatingText RandomTranslation(FloatRange xPercentage, FloatRange yPercentage) {
            var position = _rectTransform.localPosition;
            position.x = Screen.width * xPercentage.RandomPick() + position.x;
            position.y = Screen.height * xPercentage.RandomPick() + position.y;
            _rectTransform.localPosition = position;
            return this;
        }

        public FloatingText StartFloating() {
            _sequence.AppendCallback(() => Destroy(gameObject));
            _sequence.Play();
            return this;
        }

        // === Animations
        [UnityEngine.Scripting.Preserve]
        public FloatingText SetSize(Vector2 size, bool asCallback = false) {
            if (asCallback) {
                _sequence.AppendCallback(() => _rectTransform.sizeDelta = size);
            } else {
                _rectTransform.sizeDelta = size;
            }
            return this;
        }

        [UnityEngine.Scripting.Preserve]
        public FloatingText GoUpFixed(float duration, float height) {
            var destinationPosition = _rectTransform.parent.InverseTransformPoint(new Vector3(_rectTransform.position.x, height, 0));
            _sequence.Append(DOTween.To(() => _rectTransform.localPosition, SetPosition, destinationPosition, duration));
            return this;
        }
        
        [UnityEngine.Scripting.Preserve]
        public FloatingText GoUpPercentage(float duration, float percent, bool append) {
            var position = _rectTransform.position;
            var height = Screen.height / 100f * percent + position.y;
            var destinationPosition = _rectTransform.parent.InverseTransformPoint(new Vector3(position.x, height, 0));
            if (append) {
                _sequence.Append(DOTween.To(() => _rectTransform.localPosition, SetPosition, destinationPosition, duration));
            } else {
                _sequence.Join(DOTween.To(() => _rectTransform.localPosition, SetPosition, destinationPosition, duration));
            }
            return this;
        }
        
        [UnityEngine.Scripting.Preserve]
        public FloatingText BumpPercentage(float percentage, float duration, bool append) {
            var destSize = _rectTransform.sizeDelta * percentage;
            return Bump(duration, append, destSize);
        }

        public FloatingText Bump(float duration, bool append, Vector2 destSize, Ease ease = Ease.Linear) {
            if (append) {
                _sequence.Append(DOTween.To(() => _rectTransform.sizeDelta, size => _rectTransform.sizeDelta = size, destSize, duration).SetEase(ease));
            } else {
                _sequence.Join(DOTween.To(() => _rectTransform.sizeDelta, size => _rectTransform.sizeDelta = size, destSize, duration).SetEase(ease));
            }
            return this;
        }
        
        public FloatingText BumpScale(float duration, bool append, Vector3 destSize, Ease ease = Ease.Linear) {
            if (append) {
                _sequence.Append(DOTween.To(() => _rectTransform.localScale, size => _rectTransform.localScale = size, destSize, duration).SetEase(ease));
            } else {
                _sequence.Join(DOTween.To(() => _rectTransform.localScale, size => _rectTransform.localScale = size, destSize, duration).SetEase(ease));
            }
            return this;
        }
        
        public FloatingText SetTextAlpha(float duration, bool append, float value, Ease ease = Ease.Linear) {
            if (append) {
                _sequence.Append(DOTween.To(() => TextCanvasGroup.alpha, alpha => TextCanvasGroup.alpha = alpha, value, duration).SetEase(ease));
            } else {
                _sequence.Join(DOTween.To(() => TextCanvasGroup.alpha, alpha => TextCanvasGroup.alpha = alpha, value, duration).SetEase(ease));
            }
            return this;
        }

        public FloatingText FadeOut(float duration) {
            var canvasGroup = GetComponent<CanvasGroup>();
            _sequence.Append(DOTween.To(() => canvasGroup.alpha, a => canvasGroup.alpha = a, 0, duration));
            return this;
        }

        // TODO Add Camera Shake
        public FloatingText ShakeCamera(float shakeForce, float shakeDrop, bool asCallback = false) {
            return this;
        }

        public FloatingText Wait(float duration) {
            _sequence.AppendInterval(duration);
            return this;
        }

        void SetPosition(Vector3 pos) {
            var delta = Mathf.Abs(_rectTransform.localPosition.y - pos.y);
            var perlin = Mathf.PerlinNoise(0, pos.y * frequency) * 2 - 1;
            pos.x += perlin * delta;
            _rectTransform.localPosition = pos;
        }
    }
}