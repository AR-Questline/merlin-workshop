using Awaken.TG.Main.Fights.Utils;
using Awaken.TG.Main.Heroes.Items.Tooltips.Base;
using Awaken.TG.Main.Utility.Semaphores;
using Awaken.TG.MVC;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using UnityEngine;

namespace Awaken.TG.Main.Heroes.Items.Tooltips.Views {
    public class VFloatingTooltipSystemUI : View<FloatingTooltipUI>, ISemaphoreObserver {
        [SerializeField] LeftRightTooltipPositioning positioning;
        [SerializeField] CanvasGroup allGroup;

        public CanvasGroup MainCanvasGroup => allGroup;
        
        TooltipPosition _position;
        Sequence _allAppearanceSequence;
        Sequence _toCompareAppearanceSequence;
        protected FragileSemaphore _isVisible;
        
        protected override void OnInitialize() {
            _isVisible = new FragileSemaphore(false, this, Target.AppearDelay, Target.HideDelay, true);
            allGroup.alpha = 0;
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