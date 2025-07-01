using Awaken.TG.Main.AudioSystem.Notifications;
using Awaken.TG.Main.Heroes.CharacterSheet;
using Awaken.TG.Main.Heroes.CharacterSheet.Tabs;
using Awaken.TG.Main.Localization;
using Awaken.TG.Main.Scenes.SceneConstructors;
using Awaken.TG.Main.Stories;
using Awaken.TG.Main.Stories.Quests;
using Awaken.TG.Main.UI.ButtonSystem;
using Awaken.TG.Main.UI.HUD.AdvancedNotifications.MiddleScreen.Quest;
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

namespace Awaken.TG.Main.UI.HUD.AdvancedNotifications.MiddleScreen.Objective {
    public class PObjectiveNotification : PAdvancedNotification<ObjectiveNotification, PObjectiveNotificationData>, IPromptListener, IPresenterWithAccessibilityBackground {
        BetterOutlinedLabel _questName;
        BetterOutlinedLabel _objectiveUpdateText;
        VisualElement _questIcon;
        VisualPresenterKeyIcon _keyIcon;
        Prompt _openQuestLogPrompt;
        ObjectiveData _objectiveData;
        
        VisualElement IPresenterWithAccessibilityBackground.Host => Content;
        
        public PObjectiveNotification(VisualElement parent) : base(parent) { }
        
        protected override void CacheVisualElements(VisualElement contentRoot) {
            _questName = contentRoot.Q<BetterOutlinedLabel>("quest-name");
            _objectiveUpdateText = contentRoot.Q<BetterOutlinedLabel>("objective-update-text");
            _questIcon = contentRoot.Q<VisualElement>("quest-icon");
            _keyIcon = new VisualPresenterKeyIcon(contentRoot.Q<VisualElement>("key-icon"));
            
            _objectiveUpdateText.text = LocTerms.StoryObjectiveUpdated.Translate();
            
            var prompts = TargetModel.AddElement(new Prompts(null));
            _openQuestLogPrompt = Prompt.Tap(KeyBindings.UI.HUD.TrackNewQuest, LocTerms.QuestNotificationTrack.Translate(), TrackQuest);
            World.BindPresenter(TargetModel, _keyIcon, () => {
                _keyIcon.Setup(_openQuestLogPrompt);
                prompts.AddPrompt(_openQuestLogPrompt, TargetModel, this, false, false);
            });
        }

        protected override PObjectiveNotificationData GetNotificationData() {
            return PresenterDataProvider.objectiveNotificationData;
        }

        protected override NotificationSoundEvent GetNotificationSound(ObjectiveNotification notification) {
            return CommonReferences.Get.AudioConfig.ObjectiveAudio.GetSound(notification.objectiveData.objective.State);
        }

        protected override void OnBeforeShow(ObjectiveNotification notification) {
            Content.transform.position = Vector3.right * Data.InitialXOffset;
            _objectiveData = notification.objectiveData;
            _questName.text = _objectiveData.quest.DisplayName;
            
            if (_objectiveData.quest.Template.iconDescriptionReference is { IsSet: true } iconRef) {
                iconRef.RegisterAndSetup(this, _questIcon);
            } else {
                Data.DefaultQuestIcon.RegisterAndSetup(this, _questIcon);
            }
            
            RefreshQuestLogButton();
        }

        protected override void OnAfterHide() {
            _openQuestLogPrompt.SetupState(false, false);
            ReleaseReleasable();
        }

        protected override Sequence ShowSequence() {
            float fadeDuration = DebugReferences.FastNotifications ? PresenterDataProvider.DebugDurationFastFade : Data.FadeDuration;
            float moveDuration = DebugReferences.FastNotifications ? PresenterDataProvider.DebugDurationFastMove : Data.MoveDuration;
            float visibilityDuration = DebugReferences.FastNotifications ? PresenterDataProvider.DebugDurationFastVisibility : Data.VisibilityDuration;
            
            return DOTween.Sequence().SetUpdate(IsIndependentUpdate)
                .Append(Content.DoFade(1f, fadeDuration))
                .Join(Content.DoMove(Vector3.zero, moveDuration))
                .AppendInterval(visibilityDuration)
                .Append(Content.DoFade(0f, fadeDuration));
        }
        
        void TrackQuest() {
            World.Only<QuestTracker>().Track(_objectiveData.quest);
        }
        
        void RefreshQuestLogButton() {
            bool shouldBeActive = !World.Only<QuestNotificationBuffer>().IsPushing;
            _openQuestLogPrompt.SetupState(shouldBeActive, shouldBeActive);
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