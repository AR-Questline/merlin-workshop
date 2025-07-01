using DG.Tweening;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Awaken.TG.Main.Utility.UI.Feedbacks {
    public class VCCanvasGroupFade : VCSingleFeedback {
        [SerializeField, ShowIf(nameof(overrideAtPlay)), BoxGroup(VCFeedback.SpecificGroupName)] float startAlpha;
        [SerializeField, BoxGroup(VCFeedback.SpecificGroupName)] float endAlpha = 1;
        [SerializeField, BoxGroup(VCFeedback.SpecificGroupName)] CanvasGroup canvasGroup;

        protected override void PrePlaySetup() {
            canvasGroup.alpha = startAlpha;
        }
        
        protected override Tween InternalPlay() {
            return canvasGroup.DOCanvasFade(endAlpha, duration).SetEase(ease).SetUpdate(true);
        }

        void Reset() {
            canvasGroup = GetComponent<CanvasGroup>();
        }
    }
}