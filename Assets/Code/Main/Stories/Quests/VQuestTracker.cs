using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Awaken.TG.Main.Fights.Utils;
using Awaken.TG.Main.Settings;
using Awaken.TG.Main.Settings.Gameplay;
using Awaken.TG.Main.UI.HUD;
using Awaken.TG.Main.Utility.UI;
using Awaken.TG.MVC;
using Awaken.TG.MVC.Attributes;
using Awaken.TG.MVC.UI.Handlers.States;
using Awaken.TG.MVC.Utils;
using Awaken.Utility.Animations;
using Awaken.Utility.GameObjects;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using TMPro;
using UnityEngine;

namespace Awaken.TG.Main.Stories.Quests {
    [UsesPrefab("Quest/" + nameof(VQuestTracker))]
    public class VQuestTracker : View<QuestTracker> {
        const float FadeDuration = 1f;
        const float VisibilityDuration = 5f;
        
        public Transform objectivesParent;
        [SerializeField] TextMeshProUGUI questName;
        [SerializeField] CanvasGroup canvasGroup;

        readonly List<Quest> _buffer = new();
        WeakModelRef<Quest> _questInTransition;
        Tween _fadeTween;
        CancellationTokenSource _cancellationTokenSource;
        
        bool InTransition => _questInTransition.TryGet(out _);
        static bool CanBeVisible => UIStateStack.Instance.State.IsMapInteractive || World.HasAny<Story>();

        public override Transform DetermineHost() => Services.Get<ViewHosting>().OnHUD("QuestTracker");

        protected override void OnInitialize() {
            canvasGroup.alpha = 0;
            canvasGroup.TrySetActiveOptimized(false);
            Target.ListenTo(QuestTracker.Events.QuestRefreshed, quest => RefreshVisibility(quest, false).Forget(), this);
            Target.ListenTo(QuestTracker.Events.QuestTracked, quest => RefreshVisibility(quest, true).Forget(), this);
            Target.ListenTo(QuestTracker.Events.QuestTrackerClicked, OnQuestTrackerClicked, this);
            UIStateStack.Instance.ListenTo(UIStateStack.Events.UIStateChanged, OnUIStateChanged, this);
            World.Only<ShowUIHUD>().ListenTo(Setting.Events.SettingChanged, SetTrackerState, this);
        }

        async UniTaskVoid RefreshVisibility(Quest quest, bool questTracked) {
            if (quest is not { CanBeTracked: true }) {
                return;
            }

            if (await AsyncUtil.DelayFrame(this)) {
                if (questTracked) {
                    Interrupt();
                    FadeQuestTracker(quest).Forget();
                } else if (InTransition || !CanBeVisible) {
                    AddQuestToBuffer(quest);
                } else {
                    FadeQuestTracker(quest).Forget();
                }
            }
        }

        void OnQuestTrackerClicked() {
            if (InTransition) {
                Interrupt();
            } else if (Target.ActiveQuest != null) {
                FadeQuestTracker(Target.ActiveQuest).Forget();
            }
        }

        void SetTrackerState() {
            var state = _buffer.Count > 0 && World.Only<ShowUIHUD>().QuestsEnabled;
            canvasGroup.alpha = state ? 1 : 0;
            canvasGroup.TrySetActiveOptimized(state);
        }

        void OnUIStateChanged(UIState state) {
            bool hudStateAllowQuestTracker = !state.HudState.HasFlag(HUDState.QuestTrackerHidden);
            
            if (hudStateAllowQuestTracker) {
                PopNextQuest();
            } else if (InTransition) {
                Interrupt();
            }
        }

        void Interrupt() {
            _fadeTween.Kill();
            canvasGroup.alpha = 0;
            canvasGroup.TrySetActiveOptimized(false);
            Target.Trigger(QuestTracker.Events.QuestDisplayingInterrupted, _questInTransition.Get());
            FinalizeFadingQuestTracker();
        }

        async UniTaskVoid FadeQuestTracker(Quest quest) {
            canvasGroup.alpha = 0;
            canvasGroup.TrySetActiveOptimized(false);
            if (!World.Only<ShowUIHUD>().QuestsEnabled) return;
            _questInTransition = quest;
            
            Target.Trigger(QuestTracker.Events.QuestDisplayed, quest);
            questName.text = quest?.DisplayName;

            _cancellationTokenSource = new CancellationTokenSource();
                
            bool awaitResult = await AsyncUtil.DelayFrame(gameObject, 1, _cancellationTokenSource.Token);
            if (!awaitResult) return;

            FadeCanvasGroup(1f);
            
            awaitResult = await AsyncUtil.DelayTime(gameObject, FadeDuration + VisibilityDuration, _cancellationTokenSource.Token);
            if (!awaitResult) return;

            FadeCanvasGroup(0f);
            
            awaitResult = await AsyncUtil.DelayTime(gameObject, FadeDuration, _cancellationTokenSource.Token);
            if (!awaitResult) return;
            
            FinalizeFadingQuestTracker();
        }
        
        void FinalizeFadingQuestTracker() {
            _buffer.Remove(_questInTransition.Get());
            _questInTransition = null;
            _cancellationTokenSource?.Cancel();
            _cancellationTokenSource = null;
            _fadeTween = null;
            PopNextQuest();
        }

        void FadeCanvasGroup(float targetAlpha) {
            _fadeTween.Kill();
            canvasGroup.TrySetActiveOptimized(true);
            _fadeTween = canvasGroup.DOFade(targetAlpha, FadeDuration).SetUpdate(true).
                OnComplete(() => {
                    if (targetAlpha <= 0f) {
                        canvasGroup.TrySetActiveOptimized(false);
                    }
                });;
        }

        void AddQuestToBuffer(Quest quest) {
            if (_questInTransition.TryGet(out Quest q) && q == quest) {
                // --- This quest is already being displayed, refresh it's objectives
                Target.Trigger(QuestTracker.Events.QuestDisplayed, quest);
            } else if (!_buffer.Contains(quest)) {
                _buffer.Add(quest);
            }
        }

        void PopNextQuest() {
            if (InTransition) return;
            
            if (_buffer.Count > 0) {
                FadeQuestTracker(_buffer.First()).Forget();
            }
        }

        protected override IBackgroundTask OnDiscard() {
            UITweens.DiscardTween(ref _fadeTween);
            return base.OnDiscard();
        }
    }
}