using System;
using System.Threading;
using Awaken.TG.Main.Utility;
using Awaken.Utility.Maths;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using DG.Tweening.Core;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.UI;

namespace Awaken.TG.Main.Heroes.HUD {
    [Serializable]
    public class GlowEffect {
        [SerializeField] Image glowBar;
        [SerializeField] [MinMaxSlider(0, 1, true)] Vector2 alphaRange;
        [SerializeField] float glowCycleDuration = 0.25f;
        [SerializeField] [Range(-1, 10)] int flashCount = -1;
        [SerializeField] Color targetColor = Color.white;
        
        DOGetter<Color> _getter;
        DOSetter<Color> _setter;
        CancellationTokenSource _cancellationTokenSource;
        
        public bool IsPlaying { get; private set; }

        public void Init() {
            _getter = () => glowBar.color;
            _setter = value => glowBar.color = value;
            glowBar.color = targetColor.WithAlpha(0);
        }

        public void StartGlow() {
            if (IsPlaying) {
                return;
            }
            
            bool isInfinite = flashCount < 0;
            _cancellationTokenSource = new();
            GlowTask(isInfinite, _cancellationTokenSource.Token).Forget();
        }

        async UniTask GlowTask(bool isInfinite, CancellationToken cancellationToken) {
            float t = CalculateDuration(alphaRange.y);
            IsPlaying = true;
            await glowBar.DOColor(targetColor.WithAlpha(alphaRange.y), t);//.WithCancellation(cancellationToken);
            if (cancellationToken.IsCancellationRequested) {
                await GlowAlpha(CalculateDuration(0));
                IsPlaying = false;
                return;
            }

            await UniTask.NextFrame(cancellationToken);

            int counter = 0;
            while (isInfinite || counter < flashCount - 1) {
                await GlowAlpha(glowCycleDuration, alphaRange.x);//.WithCancellation(cancellationToken);
                if (cancellationToken.IsCancellationRequested) {
                    await GlowAlpha(CalculateDuration(0));
                    IsPlaying = false;
                    return;
                }

                await UniTask.NextFrame(cancellationToken);

                await GlowAlpha(glowCycleDuration, alphaRange.y);//.WithCancellation(cancellationToken);
                if (cancellationToken.IsCancellationRequested) {      
                    await GlowAlpha(CalculateDuration(0));          
                    IsPlaying = false;
                    return;
                }

                await UniTask.NextFrame(cancellationToken);

                counter++;
            }

            await GlowAlpha(glowCycleDuration);//.WithCancellation(cancellationToken);

            IsPlaying = false;
        }

        public void StopGlow() {
            if (_cancellationTokenSource == null) {
                return;
            }
            
            _cancellationTokenSource.Cancel(false);
            _cancellationTokenSource.Dispose();
            _cancellationTokenSource = null;
        }

        float CalculateDuration(float initTarget) {
            Color color = _getter.Invoke();
            float v = (alphaRange.y - alphaRange.x) / glowCycleDuration;
            float s = initTarget - color.a;
            return s / v;
        }
        
        Tween GlowAlpha(float duration, float targetValue = 0f) => DOTween.ToAlpha(_getter, _setter, targetValue, duration);
    }
}