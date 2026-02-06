using Awaken.TG.Main.Fights.Utils;
using Awaken.TG.Main.Utility.Video;
using Awaken.TG.MVC;
using Awaken.TG.MVC.Attributes;
using Awaken.TG.MVC.Events;
using Awaken.TG.MVC.UI.Handlers.Focuses;
using Awaken.Utility.Animations;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Awaken.TG.Main.Heroes.CharacterSheet.Journal.Content {
    [UsesPrefab("CharacterSheet/Journal/Content/" + nameof(VJournalTutorialVideoContent))]
    public class VJournalTutorialVideoContent : View<JournalTutorialContent>, IVideoHost {
        const float FadeDuration = 0.1f;
        
        [SerializeField] RawImage rawImage;
        [SerializeField] GameObject loadingIcon;
        [SerializeField] TMP_Text description;
        
        Sequence _textsSequence;

        public RawImage VideoDisplay => rawImage;
        public GameObject VideoTextureHolder => rawImage.gameObject;
        public Transform SubtitlesHost => null;

        public void OnVideoStarted() {
            loadingIcon.SetActive(false);
        }
        
        protected override void OnInitialize() {
            SetupDescription();
            InitVideo().Forget();
            World.EventSystem.ListenTo(EventSelector.AnySource, Focus.Events.ControllerChanged, this, SetupDescriptionWithFade);
        }
        
        void SetupDescriptionWithFade() {
            _textsSequence.Kill();

            _textsSequence = DOTween.Sequence().SetUpdate(true)
                .Append(description.DOFade(0f, FadeDuration))
                .AppendCallback(SetupDescription)
                .Append(description.DOFade(1f, FadeDuration));
        }

        void SetupDescription() {
            description.SetText(Target.Text);
        }

        async UniTaskVoid InitVideo() {
            // We need to wait to allow layout elements to place themselves correctly
            if (await AsyncUtil.DelayFrame(Target, 2) && !WasDiscarded) {
                Target.InitVideo();
            }
        }

        protected override IBackgroundTask OnDiscard() {
            _textsSequence.Kill();
            _textsSequence = null;
            Target.EndVideo();
            return base.OnDiscard();
        }
    }
}
