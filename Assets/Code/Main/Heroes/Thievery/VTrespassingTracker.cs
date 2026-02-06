using Awaken.TG.Main.Heroes.HUD;
using Awaken.TG.Main.Localization;
using Awaken.TG.MVC;
using Awaken.TG.MVC.Attributes;
using Awaken.TG.Utility;
using Awaken.Utility.GameObjects;
using DG.Tweening;
using UnityEngine;

namespace Awaken.TG.Main.Heroes.Thievery {
    [UsesPrefab("HUD/Thievery/" + nameof(VTrespassingTracker))]
    public class VTrespassingTracker : VDurationTrackerBase<TrespassingTracker> {
        const float HighlightInitialScaleX = 4f;
        
        [SerializeField] CanvasGroup infoLabelCanvasGroup;
        [SerializeField] CanvasGroup highlightImageCanvasGroup;
        
        Sequence _highlightSequence;
        
        protected override float InitialDuration => TrespassingTracker.TimeToCrime;
        protected override float MaxDuration => Target.InitialCrimeTimer;
        protected override bool DisableFade => !Target.IsTrespassing;
        protected override bool ShowTimer => Target.IsTimerStarted;
        protected override string InitialText => LocTerms.TrespassingEnteredNotification.Translate();

        protected override void InitListeners() {
            base.InitListeners();
            Target.ListenTo(TrespassingTracker.Events.TrespassingStateChanged, ChangeVisibility, this);
            Target.ListenTo(TrespassingTracker.Events.TimeToCrimeChanged, UpdateTimer, this);
            Target.ListenTo(TrespassingTracker.Events.CrimeStateChanged, UpdateCrimeState, this);
            highlightImageCanvasGroup.alpha = 0;
        }

        protected override void ChangeVisibility(bool activate) {
            base.ChangeVisibility(activate);
            
            infoLabelCanvasGroup.alpha = 0f;
            highlightImageCanvasGroup.alpha = 0f;

            if (activate) {
                highlightImageCanvasGroup.transform.localScale = new Vector3(HighlightInitialScaleX, 1f, 1f);
                _highlightSequence = DOTween.Sequence().SetUpdate(true)
                    .Append(highlightImageCanvasGroup.transform.DOScaleX(2f, 0.5f))
                    .Join(highlightImageCanvasGroup.DOFade(1f, 0.2f))
                    .Append(infoLabelCanvasGroup.DOFade(1f, 1f).SetLoops(-1, LoopType.Yoyo))
                    .Join(highlightImageCanvasGroup.transform.DOScaleX(1f, 1f))
                    .Join(highlightImageCanvasGroup.DOFade(0f, 1f));
            } else {
                _highlightSequence.Kill();
            }
        }

        void UpdateCrimeState() {
            infoLabel.text = Target.IsCrime ? LocTerms.TrespassingAlerted.Translate() : LocTerms.TrespassingEnteredNotification.Translate();
            timerBarParent.TrySetActiveOptimized(ShowTimer);
        }
    }
}
