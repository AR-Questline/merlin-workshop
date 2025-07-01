using System;
using System.Linq;
using Awaken.TG.Assets;
using Awaken.Utility.Debugging;
using Awaken.Utility.GameObjects;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.UI;
using LogType = Awaken.Utility.Debugging.LogType;
using Object = UnityEngine.Object;

namespace Awaken.TG.Main.Tutorials.Steps.Composer.Parts {
    [Serializable]
    public class PulsingPart : BasePart {
        public GameObject objectToPulse;
        public PulsePeriod pulsePeriod = PulsePeriod.Normal;
        public ScaleFactor scaleFactor = ScaleFactor.Normal;

        public bool useColor;
        [ShowIf(nameof(useColor))] public Color color = new Color(0f, 0.74f, 1f, 1f);
        
        public bool removeChildren = true;

        Tween _pulseTween;

        static Type[] s_allowedComponentTypes = {typeof(Image), typeof(RectTransform), typeof(CanvasRenderer), typeof(LazyImage)};

        public override UniTask<bool> OnRun(TutorialContext context) {
            GameObject pulseGO = SpawnPulsingObject();
            context.onFinish += () => Object.Destroy(pulseGO);
            return UniTask.FromResult(true);
        }

        public override void TestRun(TutorialContext context) {
            GameObject pulseGO = SpawnPulsingObject();
            if (pulseGO != null) {
                context.onFinish += () => {
                    _pulseTween.Kill();
                    _pulseTween = null;
                    GameObjects.DestroySafely(pulseGO);
                };
            }
        }

        GameObject SpawnPulsingObject() {
            if (objectToPulse == null) {
                Log.Important?.Error($"Null object to pulse");
                return null;
            }

            // create pulse GO
            GameObject newGO = Object.Instantiate(objectToPulse, objectToPulse.transform.parent);
            newGO.name = $"{objectToPulse.name} - Pulsing";

            // remove unnecessary components
            foreach (var component in newGO.GetComponents<Component>().ToList()) {
                if (!s_allowedComponentTypes.Contains(component.GetType())) {
                    GameObjects.DestroySafely(component);
                }
            }
            
            // remove children
            if (removeChildren) {
                GameObjects.DestroyAllChildrenSafely(newGO.transform);
            }

            // ensure image equality
            Image originalImage = objectToPulse.GetComponent<Image>();
            Image image = newGO.GetComponent<Image>();
            EnsureImageEquality(originalImage, image).Forget();
            
            // start pulse tween
            Transform trans = newGO.transform;
            float period = Period(pulsePeriod);
            float upScale = UpScale(scaleFactor);
            Color originalColor = image.color;
            Color targetColor = useColor ? color : originalColor;
            _pulseTween = DOTween.Sequence()
                .Append(DOTween.To(() => trans.localScale, v => trans.localScale = v, Vector3.one * upScale, period).SetEase(Ease.InOutCubic))
                .Join(DOTween.To(() => image.color, v => image.color = v, targetColor, period).SetEase(Ease.InOutCubic))
                .Append(DOTween.To(() => trans.localScale, v => trans.localScale = v, Vector3.one, period).SetEase(Ease.InOutCubic))
                .Join(DOTween.To(() => image.color, v => image.color = v, originalColor, period).SetEase(Ease.InOutCubic))
                .SetLoops(-1);

            return newGO;
        }

        async UniTaskVoid EnsureImageEquality(Image original, Image target) {
            Sprite sprite = original.sprite;
            while (target != null && original != null && target.gameObject != null) {
                if (original.sprite != sprite) {
                    sprite = original.sprite;
                    target.sprite = sprite;
                }
                await UniTask.DelayFrame(5);
            }
        }

        static float Period(PulsePeriod period) {
            return period switch {
                PulsePeriod.Normal => 0.7f,
                PulsePeriod.Fast => 0.4f,
                PulsePeriod.Slow => 1f,
                _ => 0.7f,
            };
        }

        static float UpScale(ScaleFactor scale) {
            return scale switch {
                ScaleFactor.Normal => 1.2f,
                ScaleFactor.NormalPlus => 1.4f,
                ScaleFactor.Big => 1.7f,
                ScaleFactor.Small => 1.1f,
                _ => 1.2f,
            };
        }

        public enum PulsePeriod {
            Normal = 0,
            Fast = 1,
            Slow = 2,
        }
        
        public enum ScaleFactor {
            Normal = 0,
            Big = 1,
            Small = 2,
            NormalPlus = 3,
        }
    }
}