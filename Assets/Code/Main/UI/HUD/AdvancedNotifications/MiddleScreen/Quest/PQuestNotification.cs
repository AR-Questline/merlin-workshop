using Awaken.TG.Main.AudioSystem.Notifications;
using Awaken.TG.Main.Localization;
using Awaken.TG.Main.Scenes.SceneConstructors;
using Awaken.TG.Main.Stories;
using Awaken.TG.Main.Stories.Quests;
using Awaken.TG.Main.UI.ButtonSystem;
using Awaken.TG.Main.UIToolkit;
using Awaken.TG.Main.UIToolkit.CustomControls;
using Awaken.TG.Main.UIToolkit.PresenterData.Notifications;
using Awaken.TG.Main.Utility;
using Awaken.TG.MVC;
using Awaken.TG.Utility;
using DG.Tweening;
using EnhydraGames.BetterTextOutline;
using UnityEngine;
using UnityEngine.UIElements;

namespace Awaken.TG.Main.UI.HUD.AdvancedNotifications.MiddleScreen.Quest {
    public class PQuestNotification : PAdvancedNotification<QuestNotification, PQuestNotificationData>, IPromptListener, IPresenterWithAccessibilityBackground {
        BetterOutlinedLabel _questName;
        BetterOutlinedLabel _questState;
        BetterOutlinedLabel _expText;
        VisualElement _questIcon;
        VisualElement _questBgIcon1;
        VisualElement _questBgIcon2;
        VisualPresenterKeyIcon _keyIcon;
        Prompt _trackQuestPrompt;
        QuestData _questData;

        VisualElement IPresenterWithAccessibilityBackground.Host => Content;
        
        public PQuestNotification(VisualElement parent) : base(parent) { }
        
        protected override void CacheVisualElements(VisualElement contentRoot) {
            _questName = contentRoot.Q<BetterOutlinedLabel>("quest-name");
            _questState = contentRoot.Q<BetterOutlinedLabel>("quest-state");
            _expText = contentRoot.Q<BetterOutlinedLabel>("exp-text");
            _questIcon = contentRoot.Q<VisualElement>("quest-icon");
            _questBgIcon1 = contentRoot.Q<VisualElement>("bg-1");
            _questBgIcon2 = contentRoot.Q<VisualElement>("bg-2");
            _keyIcon = new VisualPresenterKeyIcon(contentRoot.Q<VisualElement>("key-icon"));
            
            var prompts = TargetModel.AddElement(new Prompts(null));
            _trackQuestPrompt = Prompt.Tap(KeyBindings.UI.HUD.TrackNewQuest, LocTerms.QuestNotificationTrack.Translate(), TrackNewQuest);
            World.BindPresenter(TargetModel, _keyIcon, () => {
                _keyIcon.Setup(_trackQuestPrompt);
                prompts.AddPrompt(_trackQuestPrompt, TargetModel, this, false, false);
            });
        }

        protected override void OnFullyInitialized() {
            base.OnFullyInitialized();
            ModelUtils.DoForFirstModelOfType<QuestTracker>(qt => {
                qt.ListenTo(QuestTracker.Events.QuestRefreshed, RefreshTrackButton, this);
            }, this);
        }

        protected override PQuestNotificationData GetNotificationData() {
            return PresenterDataProvider.questNotificationData;
        }

        protected override NotificationSoundEvent GetNotificationSound(QuestNotification notification) {
            return CommonReferences.Get.AudioConfig.QuestAudio.GetSound(notification.questData.questState);
        }

        protected override void OnBeforeShow(QuestNotification notification) {
            Content.transform.position = Vector3.right * Data.InitialXOffset;
            _questBgIcon1.Hide();
            _questBgIcon2.Hide();
            
            _questData = notification.questData;
            _questName.text = _questData.questName;

            if (_questData.quest.Template.iconDescriptionReference is { IsSet: true } iconRef) {
                iconRef.RegisterAndSetup(this, _questIcon);
            } else {
                Data.DefaultQuestIcon.RegisterAndSetup(this, _questIcon);
            }

            switch (_questData.questState) {
                case QuestState.Active:
                    SetNewQuestInfo();
                    break;
                case QuestState.Completed:
                    SetCompletedQuestInfo();
                    break;
                case QuestState.Failed:
                    SetFailedQuestInfo();
                    break;
            }
            
            RefreshTrackButton();
        }

        protected override void OnAfterHide() {
            _questData = default;
            _trackQuestPrompt.SetupState(false, false);
            ReleaseReleasable();
        }

        protected override Sequence ShowSequence() {
            float fadeDuration = DebugReferences.FastNotifications ? PresenterDataProvider.DebugDurationFastFade : Data.FadeDuration;
            float moveDuration = DebugReferences.FastNotifications ? PresenterDataProvider.DebugDurationFastMove : Data.MoveDuration;
            float visibilityDuration = DebugReferences.FastNotifications ? PresenterDataProvider.DebugDurationFastVisibility : Data.VisibilityDuration;
            float startBgFadeTime = moveDuration - 0.2f;
            float bgFadeDuration = fadeDuration * 1.5f;

            return DOTween.Sequence().SetUpdate(IsIndependentUpdate)
                .Append(Content.DoFade(1f, fadeDuration))
                .Join(Content.DoMove(Vector3.zero, moveDuration))
                .AppendInterval(visibilityDuration)
                .Append(Content.DoFade(0f, fadeDuration))
                .Insert(startBgFadeTime, _questBgIcon1.DoFade(1f, bgFadeDuration))
                .Insert(startBgFadeTime + bgFadeDuration, _questBgIcon1.DoFade(0f, bgFadeDuration * 2f))
                .Insert(startBgFadeTime + bgFadeDuration / 2f, _questBgIcon2.DoFade(1f, bgFadeDuration))
                .Insert(startBgFadeTime + bgFadeDuration * 2f, _questBgIcon2.DoFade(0f, bgFadeDuration));
        }
        
        void TrackNewQuest() {
            World.Only<QuestTracker>().Track(_questData.quest);
        }
        
        void SetNewQuestInfo() {
            _questState.text = LocTerms.NewQuest.Translate().ToUpper();
            _expText.SetActiveOptimized(false);
        }
        
        void SetFailedQuestInfo() {
            _questState.text = LocTerms.FailedQuest.Translate().ToUpper();
            _expText.SetActiveOptimized(false);
        }

        void SetCompletedQuestInfo() {
            _questState.text = LocTerms.CompletedQuest.Translate().ToUpper();
            string gainedXP = QuestUtils.GetGainedXPInfo(_questData.gainedXP);
            _expText.text = gainedXP.ToUpper();
            _expText.SetActiveOptimized(true);
        }
        
        void RefreshTrackButton() {
            bool shouldBeActive = _questData is { questState: QuestState.Active, quest: { ShowNotificationTrackPrompt: true } }
                                  && World.Only<QuestTracker>().ActiveQuest != _questData.quest;
            _trackQuestPrompt.SetupState(shouldBeActive, shouldBeActive);
        }

        // --- IPromptListener
        public void SetName(string name) { }

        public void SetActive(bool active) { }

        public void SetVisible(bool visible) {
            if (visible) {
                _keyIcon.Content.ShowAndSetActiveOptimized();
            } else {
                _keyIcon.Content.HideAndSetActiveOptimized();
            }
        }
    }
}