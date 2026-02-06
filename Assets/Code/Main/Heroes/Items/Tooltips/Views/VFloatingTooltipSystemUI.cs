using Awaken.TG.Main.Fights.Utils;
using Awaken.TG.Main.Heroes.Items.Tooltips.Base;
using Awaken.TG.Main.Utility.Semaphores;
using Awaken.TG.MVC;
using ChocDino.UIFX;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using Sirenix.OdinInspector;
using TMPro;
using UnityEngine;

namespace Awaken.TG.Main.Heroes.Items.Tooltips.Views {
    public class VFloatingTooltipSystemUI : View<FloatingTooltipUI>, ISemaphoreObserver {
        [SerializeField] LeftRightTooltipPositioning positioning;
        [SerializeField] CanvasGroup allGroup;
        [SerializeField, FoldoutGroup("Highlight")] bool useHighlight;
        [SerializeField, FoldoutGroup("Highlight"), ShowIf(nameof(useHighlight))] TextMeshProUGUI highlightInfoText;
        [SerializeField, FoldoutGroup("Highlight"), ShowIf(nameof(useHighlight))] OutlineFilter highlightBlurOutlineFilter;
        [SerializeField, FoldoutGroup("Highlight"), ShowIf(nameof(useHighlight))] float highlightSize = 32f;
        [SerializeField, FoldoutGroup("Highlight"), ShowIf(nameof(useHighlight))] float highlightDuration = 0.5f;

        public CanvasGroup MainCanvasGroup => allGroup;
        
        Sequence _highlightSequence;
        Sequence _allAppearanceSequence;
        protected FragileSemaphore _isVisible;
        
        protected override void OnInitialize() {
            _isVisible = new FragileSemaphore(false, this, Target.AppearDelay, Target.HideDelay, true);
            allGroup.alpha = 0;
            HideHighlight();
        }

        void Update() {
            _isVisible.Update();
        }
        
        public void SetPosition(TooltipPosition left, TooltipPosition right) {
            positioning.SetPosition(left, right);
            if (_isVisible) {
                positioning.RefreshPosition();
            }
        }
        
        public void ForceDisappear() {
            DisappearSequence();
        }

        protected virtual void RefreshPosition() {
            if (positioning.IsValid) {
                positioning.RefreshPosition();
            }
        }

        protected virtual bool TryAppear() {
            return true;
        }
        
        protected Tween FadeGroup(CanvasGroup group, float alpha) {
            return DOTween.To(() => group.alpha, a => group.alpha = a, alpha, Target.AlphaTweenTime);
        }

        protected void HighlightTooltip(string highlightText = "") {
            if (!useHighlight) {
                return;
            }

            if (highlightInfoText) {
                highlightInfoText.SetText(highlightText);
            }
            
            _highlightSequence.Kill(true);
            _highlightSequence = DOTween.Sequence().SetUpdate(true)
                .Append(DOVirtual.Float(0f, highlightSize, highlightDuration, x => highlightBlurOutlineFilter.Size = x).SetEase(Ease.OutCubic))
                .Join(FadeInHighlightText())
                .Append(DOVirtual.Float(highlightSize, 0f, highlightDuration, x => highlightBlurOutlineFilter.Size = x).SetEase(Ease.OutSine))
                .Join(FadeOutHighlightText())
                .OnComplete(HideHighlight);
        }
        
        protected void HideHighlight() {
            _highlightSequence.Kill();
            if (highlightBlurOutlineFilter) {
                highlightBlurOutlineFilter.Size = 0f;
            }
            
            if (highlightInfoText) {
                highlightInfoText.alpha = 0f;
            }
        }

        Tween FadeInHighlightText() {
            return !highlightInfoText ? null : highlightInfoText.DOFade(1f, highlightDuration);
        }

        Tween FadeOutHighlightText() {
            return !highlightInfoText ? null : highlightInfoText.DOFade(0f, highlightDuration * 2f).SetDelay(highlightDuration / 3f);
        }
        
        async UniTaskVoid Appear() {
            if(TryAppear() == false) {
                return;
            }
            
            _allAppearanceSequence.Kill();
            _allAppearanceSequence = null;

            if (!await AsyncUtil.DelayFrame(this, 3)) {
                return;
            }

            RefreshPosition();
            
            if (_allAppearanceSequence != null) {
                return;
            }
            _allAppearanceSequence = DOTween.Sequence().SetUpdate(true)
                .Append(FadeGroup(allGroup, 1));
        }

        void Disappear() {
            if (Target.PreventDisappearing) {
                return;
            }
            
            DisappearSequence();
        }
        
        void DisappearSequence() {
            _isVisible.Set(false);
            _allAppearanceSequence.Kill();
            _allAppearanceSequence = DOTween.Sequence().SetUpdate(true)
                .Append(FadeGroup(allGroup, 0));
        }
        
        void ISemaphoreObserver.OnUp() => Appear().Forget();
        void ISemaphoreObserver.OnDown() => Disappear();
    }
}