using System;
using DG.Tweening;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.UI;

namespace Awaken.TG.Main.Utility.UI.Feedbacks {
    public class VCHighlightMapMarker : VCFeedback {
        [SerializeField, BoxGroup(VCFeedback.GeneralGroupName)] Image image;
        [SerializeField, BoxGroup(VCFeedback.GeneralGroupName)] float startAlpha;
        [SerializeField, BoxGroup(VCFeedback.GeneralGroupName)] float startDelay;
        [SerializeField] FeedbackStep[] feedbackSteps = Array.Empty<FeedbackStep>();
        
        Sequence _currentSequence;

        protected override void OnAttach() {
            InitFeedbackImage();
            base.OnAttach();
        }

        void InitFeedbackImage() {
            image.color = new Color(image.color.r, image.color.g, image.color.b, startAlpha);
        }
        
        protected override Tween InternalPlay() {
            if (_currentSequence != null) {
                Stop();
            }
            
            _currentSequence = DOTween.Sequence().SetUpdate(true);
            _currentSequence.AppendInterval(startDelay);

            foreach (var step in feedbackSteps) {
                _currentSequence.Append(step.Execute(image));
            }

            return _currentSequence;
        }

        protected override void InternalStop() {
            InitFeedbackImage();
        }
        
        [Serializable]
        struct FeedbackStep {
            [SerializeField] float fadeDuration;
            [SerializeField] float stepDelay;
            [SerializeField] FadeType fadeType;
            
            [SerializeField] bool loop;
            [SerializeField, ShowIf(nameof(loop))] int loopCount;
            [SerializeField, ShowIf(nameof(loop))] float tweenInLoopDuration;
            [SerializeField, ShowIf(nameof(loop))] float delayBetweenLoops;
            [SerializeField, ShowIf(nameof(loop))] FadeType loopFadeType;
            
            public Tween Execute(Image image) {
                var sequence = DOTween.Sequence().SetUpdate(true);
                
                if (loop) {
                    for (int i = 0; i < loopCount; i++) {
                        SetupSequence(sequence, image, loopFadeType);
                        
                        if (i < loopCount - 1) {
                            sequence.AppendInterval(delayBetweenLoops);
                        }
                    }
                } else {
                    SetupSequence(sequence, image, fadeType);
                }
                
                return sequence.AppendInterval(stepDelay);
            }

            void SetupSequence(Sequence sequence, Image image, FadeType fadeType) {
                if (fadeType == FadeType.FadeIn) {
                    sequence.Append(image.DOFade(0, fadeDuration));
                }
                else if (fadeType == FadeType.FadeOut) {
                    sequence.Append(image.DOFade(1, fadeDuration));
                }
                else if (fadeType == FadeType.FadeInOut) {
                    sequence.Append(image.DOFade(0, fadeDuration));
                    sequence.Append(image.DOFade(1, fadeDuration));
                }
                else if (fadeType == FadeType.FadeOutIn) {
                    sequence.Append(image.DOFade(1, fadeDuration));
                    sequence.Append(image.DOFade(0, fadeDuration));
                }
            }

            enum FadeType : byte {
                FadeIn,
                FadeOut,
                FadeInOut,
                FadeOutIn
            }
        }
    }
}
