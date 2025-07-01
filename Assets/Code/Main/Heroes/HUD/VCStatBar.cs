using Awaken.TG.Main.Heroes.HUD.Bars;
using Awaken.TG.Main.Heroes.Stats;
using Awaken.TG.Main.Utility.Semaphores;
using Awaken.TG.MVC;
using Awaken.TG.Utility;
using DG.Tweening;
using JetBrains.Annotations;
using TMPro;
using UnityEngine;

namespace Awaken.TG.Main.Heroes.HUD {
    public abstract class VCStatBar<T> : ViewComponent<T>, ISemaphoreObserver where T : IModel {
        const float FadeDuration = 0.15f;
        const float DisappearDelay = 2f;
        
        [SerializeField] Bar bar;
        [SerializeField] CanvasGroup group;
        [SerializeField] bool deactivateGameObjectOnHide;
        [SerializeField, CanBeNull] TMP_Text statValue;
        
        FragileSemaphore _hideSemaphore;
        Tween _barTween;

        protected abstract StatType StatType { get; }
        protected abstract float Percentage { get; }
        protected virtual string StatText => "{0}";
        protected virtual bool ShouldHide => Percentage > 0.99f;
        protected virtual float ShowOnStatValueChangeThreshold => M.Epsilon;
        protected virtual IWithStats TargetWithStats => Target as IWithStats;
        
        protected Bar Bar => bar;
        bool IsShown => !_hideSemaphore.DesiredState;
        protected int StatValue => TargetWithStats?.Stat(StatType)?.ModifiedInt ?? 0;

        void Start() {
            group.alpha = 0;
            if (deactivateGameObjectOnHide) {
                group.gameObject.SetActive(false);
            }
            _hideSemaphore = new FragileSemaphore(true, this, DisappearDelay, true);
        }

        protected override void OnAttach() {
            TargetWithStats.ListenTo(Stat.Events.StatChangedBy(StatType), OnStatChange, this);
            Bar.SetPercentInstant(Percentage);
            _hideSemaphore.ForceTrue();
        }

        void Update() {
            if (HasBeenDiscarded || !Target.IsInitialized) return;
            
            SetBarPercentage();
            if (statValue != null) {
                statValue.text = RichTextUtil.SmartFormatParams(StatText, StatValue.ToString());
            }
            
            if (IsShown && ShouldHide) {
                _hideSemaphore.Set(true);
            } else if (!IsShown && !ShouldHide) {
                _hideSemaphore.Set(false);
            }
            _hideSemaphore.Update();
        }

        protected virtual void SetBarPercentage() {
            Bar.SetPercent(Percentage);
        }

        // === Callbacks
        void OnStatChange(Stat.StatChange change) {
            if (Mathf.Abs(change.value) >= ShowOnStatValueChangeThreshold) {
                _hideSemaphore.Set(false);
            }
        }

        void ISemaphoreObserver.OnUp() => HideBar();
        void ISemaphoreObserver.OnDown() => ShowBar();

        // === Tweens
        protected void HideBar() {
            _barTween = FadeCanvasGroup(group, 0f, FadeDuration).OnComplete(() => {
                if (deactivateGameObjectOnHide) {
                    group.gameObject.SetActive(false);
                }
            });
        }

        protected void ShowBar() {
            if (deactivateGameObjectOnHide) {
                group.gameObject.SetActive(true);
            }
            _barTween = FadeCanvasGroup(group, 1f, FadeDuration);
        }

        Tween FadeCanvasGroup(CanvasGroup canvasGroup, float endValue, float duration) {
            _barTween?.Complete(true);
            return canvasGroup.DOFade(endValue, duration).SetUpdate(true);
        }
    }
}