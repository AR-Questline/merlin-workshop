using Awaken.TG.Main.Fights.Utils;
using Awaken.TG.Main.Utility.Video;
using Awaken.TG.MVC;
using Awaken.TG.MVC.Attributes;
using Awaken.Utility.Animations;
using Cysharp.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Awaken.TG.Main.Heroes.CharacterSheet.Journal.Content {
    [UsesPrefab("CharacterSheet/Journal/Content/" + nameof(VJournalTutorialVideoContent))]
    public class VJournalTutorialVideoContent : View<JournalTutorialContent>, IVideoHost {
        [SerializeField] RawImage rawImage;
        [SerializeField] GameObject loadingIcon;
        [SerializeField] TMP_Text description;

        public RawImage VideoDisplay => rawImage;
        public GameObject VideoTextureHolder => rawImage.gameObject;
        public Transform SubtitlesHost => null;

        public void OnVideoStarted() {
            loadingIcon.SetActive(false);
        }
        
        protected override void OnInitialize() {
            description.SetText(Target.Text);
            InitVideo().Forget();
        }

        async UniTaskVoid InitVideo() {
            // We need to wait to allow layout elements to place themselves correctly
            if (await AsyncUtil.DelayFrame(Target, 2) && !WasDiscarded) {
                Target.InitVideo();
            }
        }

        protected override IBackgroundTask OnDiscard() {
            Target.EndVideo();
            return base.OnDiscard();
        }
    }
}
