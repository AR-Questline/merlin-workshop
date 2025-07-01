using Awaken.TG.Main.Heroes.Statuses;
using Awaken.TG.Main.Heroes.Statuses.BuildUp;
using Awaken.TG.Main.Utility.UI;
using Awaken.TG.MVC;
using Awaken.Utility.GameObjects;
using DG.Tweening;
using TMPro;
using UnityEngine;

namespace Awaken.TG.Main.Heroes.CharacterSheet {
    public class VCHeroStatusAnnouncer : ViewComponent<Hero> {
        [SerializeField] float fadeDuration = 0.5f;
        [SerializeField] float stayDuration = 0.1f;
        [SerializeField] float longStayDuration = 5f;
        [SerializeField] CanvasGroup canvasGroup;
        [SerializeField] TextMeshProUGUI statusText;

        bool _isAnnouncing;
        Sequence _sequence;

        protected override void OnAttach() {
            FadeOutImmediate();
            Target.AfterFullyInitialized(() => {
                Target.Statuses.ListenTo(CharacterStatuses.Events.AddedStatus, OnStatusAddedToHero, this);
            });
        }
        
        void OnStatusAddedToHero(Status addedStatus) {
            if (_isAnnouncing || addedStatus.HiddenOnUI || !IsForeignApplier(addedStatus)) {
                return;
            }

            if (addedStatus is BuildupStatus buildupStatus) {
                buildupStatus.ListenToLimited(BuildupStatus.Events.BuildupCompleted, OnBuildupCompleted, this);
                return;
            }
            
            Announce(addedStatus);
        }

        void OnBuildupCompleted(BuildupStatus buildupStatus) {
            switch (buildupStatus) {
                case BuildupStatusUpgradable:
                    return;
                case BuildupStatusActivation:
                    Announce(buildupStatus);
                    break;
            }
        }

        void OnStatusDiscarded() {
            _isAnnouncing = false;
            Status statusToShow = Hero.Current.Statuses.Elements<Status>().FirstOrDefault(s => !s.HiddenOnUI && !s.Type.IsPositive && IsForeignApplier(s) && s is not BuildupStatus);
            if (statusToShow != null) {
                Announce(statusToShow);
                return;
            }
            
            _sequence?.Kill(true);
            _sequence = FadeOut();
        }

        void Announce(Status status) {
            if (_isAnnouncing) {
                return;
            }
            
            statusText.SetText(status.Template.displayName.ToString());
            _sequence?.Kill(true);
            _sequence = status.Type.IsPositive ? ShowSequence() : BlinkSequence();

            if (!status.Type.IsPositive) {
                _isAnnouncing = true; // only negative statuses block announcing. positive status will be overriden by negative since it's more important
                status.ListenToLimited(Model.Events.AfterDiscarded, OnStatusDiscarded, this);
            }
        }

        Sequence BlinkSequence() {
            canvasGroup.TrySetActiveOptimized(true);
            return DOTween.Sequence().SetUpdate(true).SetLoops(-1, LoopType.Yoyo)
                .Append(canvasGroup.DOFade(1, fadeDuration))
                .AppendInterval(stayDuration)
                .OnKill(FadeOutImmediate);
        }
        
        Sequence ShowSequence() {
            canvasGroup.TrySetActiveOptimized(true);
            return DOTween.Sequence().SetUpdate(true)
                .Append(canvasGroup.DOFade(1, fadeDuration))
                .AppendInterval(longStayDuration)
                .Append(canvasGroup.DOFade(0, fadeDuration))
                .OnKill(FadeOutImmediate);
        }

        Sequence FadeOut() {
            return DOTween.Sequence(canvasGroup.DOFade(0, fadeDuration)).SetUpdate(true).OnComplete(() => canvasGroup.TrySetActiveOptimized(false));
        }
        
        void FadeOutImmediate() {
            _isAnnouncing = false;
            canvasGroup.alpha = 0;
            canvasGroup.TrySetActiveOptimized(false);
        }

        static bool IsForeignApplier(Status addedStatus) {
            return !addedStatus.SourceInfo.SourceCharacter.TryGet(out var character) || character is not Hero;
        }

        protected override void OnDiscard() {
            UITweens.DiscardSequence(ref _sequence, true);
        }
    }
}