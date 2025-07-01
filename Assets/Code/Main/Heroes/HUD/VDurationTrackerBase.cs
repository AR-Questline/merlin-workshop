using Awaken.TG.Main.Heroes.HUD.Bars;
using Awaken.TG.Main.UI.HUD;
using Awaken.TG.MVC;
using Awaken.TG.MVC.UI.Handlers.States;
using Awaken.Utility.Extensions;
using DG.Tweening;
using TMPro;
using UnityEngine;

namespace Awaken.TG.Main.Heroes.HUD {
    public abstract class VDurationTrackerBase<T> : View<T> where T : IModel {
        protected const float FadeDuration = 0.5f;
        
        [SerializeField] protected TMP_Text infoLabel;
        [SerializeField] protected CanvasGroup content;
        [SerializeField] Bar timerBar;
        [SerializeField] protected CanvasGroup timerCanvasGroup;

        Tween _contentTween;
        CanvasGroup _canvasGroup;
        
        protected abstract float InitialDuration { get; } 
        protected abstract float MaxDuration { get; }
        protected abstract bool DisableFade { get; }
        protected abstract bool ShowTimer { get; }
        protected abstract string InitialText { get; }
        
        public override Transform DetermineHost() => Services.Get<ViewHosting>().OnHUD();
        
        protected override void OnInitialize() {
            _canvasGroup = GetComponent<CanvasGroup>();
            content.alpha = 0;
            infoLabel.text = InitialText;
            
            InitListeners();
            UpdateTimer(InitialDuration);
        }

        protected virtual void InitListeners() {
            UIStateStack.Instance.ListenTo(UIStateStack.Events.UIStateChanged, OnUIStateChanged, this);
        }
        
        void OnUIStateChanged(UIState state) {
            bool hudStateAllowsCompass = !state.HudState.HasFlag(HUDState.CompassHidden);
            _canvasGroup.alpha = hudStateAllowsCompass ? 1 : 0;
            
            if (DisableFade || IsActiveAndShouldBeActive() || IsHiddenAndShouldBeHidden()) {
                return;
            }

            FadeContent(state.IsMapInteractive ? 1 : 0);
            return;
            
            bool IsActiveAndShouldBeActive() => content.alpha == 1 && state.IsMapInteractive;
            bool IsHiddenAndShouldBeHidden() => content.alpha == 0 && !state.IsMapInteractive;
        }
        
        protected void ChangeVisibility(bool activate) {
            FadeContent(activate ? 1 : 0);
            timerCanvasGroup.alpha = ShowTimer ? 1 : 0;
        }

        protected void UpdateTimer(float value) {
            float barValue = value / MaxDuration;
            timerBar.SetPercent(barValue);
        }

        void FadeContent(float targetAlpha) {
            _contentTween.KillWithoutCallback();
            _contentTween = content
                .DOFade(targetAlpha, FadeDuration)
                .SetAutoKill(false)
                .OnKill(() => content.alpha = 0);
        }
    }
}
