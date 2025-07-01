using System.Text;
using System.Threading;
using Awaken.TG.Main.AudioSystem.Notifications;
using Awaken.TG.Main.Fights.Utils;
using Awaken.TG.Main.Scenes.SceneConstructors;
using Awaken.TG.Main.Stories.Quests.Objectives;
using Awaken.TG.Main.Utility.UI;
using Awaken.TG.MVC;
using Awaken.TG.MVC.Attributes;
using Awaken.Utility;
using Awaken.Utility.Animations;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Cysharp.Threading.Tasks;

namespace Awaken.TG.Main.Stories.Quests {
    [UsesPrefab("Quest/" + nameof(VQuestTrackerObjective))]
    public class VQuestTrackerObjective : View<QuestTrackerObjective> {
        [SerializeField] TextMeshProUGUI questObjectiveName;
        [SerializeField] CanvasGroup canvasGroup;
        [SerializeField] HorizontalLayoutGroup horizontalLayoutGroup;
        [SerializeField] LayoutElement layoutElement;
        [SerializeField] RectTransform rectTransform;
        [SerializeField] GameObject tick;
        
        const float StrikethroughDuration = 1.5f;
        bool _inTransition;
        bool _strikeThroughEnded;
        Sequence _sequence;
        CancellationTokenSource _cancellationTokenSource;

        public override Transform DetermineHost() => Target.ParentModel.View<VQuestTracker>().objectivesParent;

        protected override void OnInitialize() {
            questObjectiveName.text = Target.objective.GetQuestTrackerDescription();
            Hide();
            
            Target.ParentModel.ListenTo(QuestTracker.Events.QuestDisplayed, OnQuestDisplayed, this);
            Target.ParentModel.ListenTo(QuestTracker.Events.QuestDisplayingInterrupted, OnQuestDisplayingInterrupted, this);
        }

        void OnQuestDisplayingInterrupted() {
            if (!_inTransition) {
                return;
            }
            
            _cancellationTokenSource.Cancel();
            _cancellationTokenSource = null;
            Hide();
            _inTransition = false;
            Target.Discard();
        }

        void OnQuestDisplayed(Quest quest) {
            if (Target.objective.ParentModel != quest) {
                Hide();
                return;
            }
            
            if (_inTransition) {
                return;
            }
            
            if (_strikeThroughEnded && Target.objective.State is ObjectiveState.Completed or ObjectiveState.Failed) {
                return;
            }
            
            questObjectiveName.text = Target.objective.GetQuestTrackerDescription();
            Show();
            if (Target.objective.State is ObjectiveState.Completed or ObjectiveState.Failed) {
                StrikethroughObjective(Target.objective.State).Forget();
            }
            var sound = CommonReferences.Get.AudioConfig.ObjectiveAudio.GetSound(Target.objective.State);
            if (!sound.eventReference.IsNull) {
                Services.Get<NotificationsAudioService>().PlayNotificationSound(sound);
            }
        }
        
        async UniTaskVoid StrikethroughObjective(ObjectiveState objectiveState) {
            _inTransition = true;
            _cancellationTokenSource?.Cancel();
            _cancellationTokenSource = new CancellationTokenSource();

            if (!await AsyncUtil.DelayFrame(gameObject, 2, _cancellationTokenSource.Token)) {
                return;
            }

            layoutElement.minHeight = rectTransform.rect.height;
            horizontalLayoutGroup.enabled = false;
            
            const string StrikethroughEndTag = "</s>";
            var word = Target.objective.GetQuestTrackerDescription();
            var stringBuilder = new StringBuilder($"<s>{word}");
            int insertIndex = 4; //skip "<s>" tag
            float stepDuration = StrikethroughDuration / word.Length;
            
            bool awaitResult = await AsyncUtil.DelayTime(gameObject, 0.5f, _cancellationTokenSource.Token);
            if (!awaitResult) return;

            var entryColor = objectiveState == ObjectiveState.Completed ? ARColor.SecondaryAccent : ARColor.DarkerGrey;
            questObjectiveName.DOColor(entryColor, StrikethroughDuration);
            for (int i = 0; i < word.Length; i++) {
                if (insertIndex >= stringBuilder.Length) {
                    break;
                }
                stringBuilder.Replace(StrikethroughEndTag, "");
                stringBuilder.Insert(insertIndex, StrikethroughEndTag);
                insertIndex++;
                questObjectiveName.text = stringBuilder.ToString();
                
                awaitResult = await AsyncUtil.DelayTime(gameObject, stepDuration, _cancellationTokenSource.Token);
                if (!awaitResult) return;
            }

            RunSequence();
            
            awaitResult = await AsyncUtil.DelayTime(gameObject, 1f, _cancellationTokenSource.Token);
            if (!awaitResult) return;

            _cancellationTokenSource.Cancel();
            _cancellationTokenSource = null;
            _inTransition = false;
            _strikeThroughEnded = true;
            if (!Target.objective.CanBeCompletedMultipleTimes) {
                Target.Discard();
            }
        }

        void RunSequence() {
            var localScale = transform.localScale;
            _sequence.Kill();
            _sequence = DOTween.Sequence().SetUpdate(true)
                .Join(DOTween.To(() => layoutElement.minHeight, h => layoutElement.minHeight = h, 0f, 1f).SetEase(Ease.InOutQuint))
                .Join(DOTween.To(() => localScale.y, y => {
                    localScale.y = y;
                    transform.localScale = localScale;
                }, 0f, 1f).SetEase(Ease.InOutQuint))
                .Join(DOTween.To(() => canvasGroup.alpha, a => canvasGroup.alpha = a, 0f, 1f).SetEase(Ease.InOutQuint));
        }
        
        // === Helpers
        void Hide() {
            canvasGroup.alpha = 0;
            layoutElement.minHeight = 0;
            layoutElement.preferredHeight = 0;
            gameObject.SetActive(false);
            tick.SetActive(false);
        }

        void Show() {
            tick.SetActive(Target.objective.State is ObjectiveState.Completed);
            gameObject.SetActive(true);
            canvasGroup.alpha = 1;
            layoutElement.preferredHeight = -1;
            transform.localScale = Vector3.one;
            horizontalLayoutGroup.enabled = true;
            questObjectiveName.color = ARColor.LightGrey;
        }

        protected override IBackgroundTask OnDiscard() {
            UITweens.DiscardSequence(ref _sequence);
            return base.OnDiscard();
        }
    }
}