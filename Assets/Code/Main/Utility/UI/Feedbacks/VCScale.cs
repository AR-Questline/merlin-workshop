using DG.Tweening;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Awaken.TG.Main.Utility.UI.Feedbacks {
    public class VCScale : VCSingleFeedback {
        [SerializeField, ShowIf(nameof(overrideAtPlay)), BoxGroup(VCFeedback.SpecificGroupName)] float startScale;
        [SerializeField, BoxGroup(VCFeedback.SpecificGroupName)] float endScale = 1;
        [SerializeField, BoxGroup(VCFeedback.SpecificGroupName)] Transform scaleTarget;

        protected override void PrePlaySetup() {
            scaleTarget.localScale =  Vector3.one * startScale;
        }
        
        protected override Tween InternalPlay() {
            return scaleTarget.DOScale(Vector3.one * endScale, duration)
                .SetEase(ease)
                .SetDelay(delay)
                .SetLoops(loops, loopType)
                .SetUpdate(true);
        }

        void Reset() {
            scaleTarget = GetComponent<Transform>();
        }
    }
}